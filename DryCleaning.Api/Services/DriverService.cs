using DryCleaning.Api.Common;
using DryCleaning.Api.Data;
using DryCleaning.Api.Domain;
using DryCleaning.Api.Dtos;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace DryCleaning.Api.Services;

/// <summary>
/// Shared marketplace drivers (no ServiceProviderId). Jobs:
/// Pickup  (CustomerToProvider): accept → verify pickup code → hand over to provider.
/// Delivery (ProviderToCustomer): accept → collect from provider → deliver with customer OTP.
/// </summary>
public sealed class DriverService(AppDbContext db, CurrentProfile profile, IOptions<MarketplaceOptions> marketplace)
{
    private MarketplaceOptions Market => marketplace.Value;

    public async Task<DriverProfileDto> GetProfileAsync(CancellationToken ct)
    {
        var driver = await profile.DriverAsync(ct);
        var user = await db.Users.FirstAsync(u => u.Id == driver.UserId, ct);
        return new DriverProfileDto
        {
            Id = driver.Id,
            User = user.ToDto(driver.Id, ProfileStatusExtensions.StatusOf(driver.IsActive, driver.ApprovedAt)),
            LicenseNumber = driver.LicenseNumber,
            IsAvailable = driver.IsAvailable,
            IsActive = driver.IsActive,
            Address = driver.Address?.ToDto()
        };
    }

    public async Task<DriverProfileDto> UpdateProfileAsync(UpdateDriverProfileRequest request, CancellationToken ct)
    {
        var driver = await profile.DriverAsync(ct);
        driver.LicenseNumber = request.LicenseNumber.Trim();

        var user = await db.Users.FirstAsync(u => u.Id == driver.UserId, ct);
        if (!string.IsNullOrWhiteSpace(request.Name))
            user.Name = request.Name.Trim();
        if (request.Email is not null)
        {
            var email = string.IsNullOrWhiteSpace(request.Email) ? null : request.Email.Trim().ToLowerInvariant();
            if (email is not null && await db.Users.AnyAsync(u => u.Id != user.Id && u.Email == email, ct))
                throw new ConflictException("That email address is already in use.");
            user.Email = email;
        }
        if (!string.IsNullOrWhiteSpace(request.Phone))
        {
            var phone = request.Phone.Trim();
            if (await db.Users.AnyAsync(u => u.Id != user.Id && u.Phone == phone, ct))
                throw new ConflictException("That phone number is already in use.");
            user.Phone = phone;
        }

        await db.SaveChangesAsync(ct);
        return await GetProfileAsync(ct);
    }

    public async Task<DriverDashboardDto> GetDashboardAsync(CancellationToken ct)
    {
        var driver = await profile.DriverAsync(ct);
        var user = await db.Users.FirstAsync(u => u.Id == driver.UserId, ct);
        var available = driver.IsAvailable ? await AvailableQuery().CountAsync(ct) : 0;
        var active = await JobQuery()
            .Where(j => j.DriverId == driver.Id && (j.Status == DriverJobStatus.Assigned || j.Status == DriverJobStatus.InProgress))
            .OrderBy(j => j.AssignedAt)
            .FirstOrDefaultAsync(ct);

        var today = DateTime.UtcNow.Date;
        var todaysJobs = await JobQuery()
            .Where(j => j.DriverId == driver.Id && ((j.AssignedAt >= today) || (j.CompletedAt >= today)))
            .OrderByDescending(j => j.CompletedAt ?? j.AssignedAt)
            .Take(20)
            .ToListAsync(ct);
        var completedToday = todaysJobs.Count(j => j.Status == DriverJobStatus.Completed && j.CompletedAt >= today);
        var unread = await db.Notifications.CountAsync(n => n.UserId == driver.UserId && !n.IsRead, ct);

        return new DriverDashboardDto
        {
            DriverName = user.Name,
            IsAvailable = driver.IsAvailable,
            AvailableJobs = available,
            ActiveJob = active?.ToDetail(driver.Id, Market.DriverEarningPerJob),
            CompletedToday = completedToday,
            EarningsToday = completedToday * Market.DriverEarningPerJob,
            UnreadNotifications = unread,
            TodaysJobs = todaysJobs.Select(j => j.ToDto(j.Order, Market.DriverEarningPerJob)).ToList(),
            Currency = Market.Currency
        };
    }

    public async Task<bool> SetAvailabilityAsync(bool isAvailable, CancellationToken ct)
    {
        var driver = await profile.DriverAsync(ct);
        if (!driver.IsActive)
            throw new ForbiddenException(driver.ApprovedAt is null
                ? "Your driver account is still under review. You can go online once an admin approves it."
                : "Your driver account has been deactivated. Please contact support.");
        driver.IsAvailable = isAvailable;
        await db.SaveChangesAsync(ct);
        return driver.IsAvailable;
    }

    // ── D02 Jobs ─────────────────────────────────────────────────────────────

    public async Task<PagedResult<DriverJobDto>> GetAvailableJobsAsync(PagedRequest request, CancellationToken ct)
    {
        var driver = await profile.DriverAsync(ct);
        if (!driver.IsAvailable)
            return new PagedResult<DriverJobDto> { Items = [], Page = request.Page, PageSize = request.PageSize, TotalCount = 0 };

        var (items, total) = await AvailableQuery().OrderBy(j => j.CreatedAt).PageAsync(request, ct);
        return new PagedResult<DriverJobDto>
        {
            Items = items.Select(j => j.ToDto(j.Order, Market.DriverEarningPerJob)).ToList(),
            Page = request.Page,
            PageSize = request.PageSize,
            TotalCount = total
        };
    }

    public async Task<PagedResult<DriverJobDto>> GetMyJobsAsync(DriverJobsQuery query, CancellationToken ct)
    {
        var driver = await profile.DriverAsync(ct);
        var jobs = JobQuery().Where(j => j.DriverId == driver.Id);
        if (query.Status is not null)
            jobs = jobs.Where(j => j.Status == query.Status);

        var (items, total) = await jobs.OrderByDescending(j => j.CompletedAt ?? j.AssignedAt).PageAsync(query, ct);
        return new PagedResult<DriverJobDto>
        {
            Items = items.Select(j => j.ToDto(j.Order, Market.DriverEarningPerJob)).ToList(),
            Page = query.Page,
            PageSize = query.PageSize,
            TotalCount = total
        };
    }

    /// <summary>Badge counts for the Available / My Jobs / Completed tabs (Available is 0 while offline).</summary>
    public async Task<DriverJobCountsDto> GetJobCountsAsync(CancellationToken ct)
    {
        var driver = await profile.DriverAsync(ct);
        var available = driver.IsAvailable ? await AvailableQuery().CountAsync(ct) : 0;
        var mine = await db.DriverJobs.CountAsync(j => j.DriverId == driver.Id &&
            (j.Status == DriverJobStatus.Assigned || j.Status == DriverJobStatus.InProgress), ct);
        var completed = await db.DriverJobs.CountAsync(j => j.DriverId == driver.Id && j.Status == DriverJobStatus.Completed, ct);
        return new DriverJobCountsDto { Available = available, Mine = mine, Completed = completed };
    }

    public async Task<DriverJobDetailDto> GetJobAsync(Guid jobId, CancellationToken ct)
    {
        var driver = await profile.DriverAsync(ct);
        var job = await JobQuery().FirstOrDefaultAsync(j => j.Id == jobId && (j.DriverId == driver.Id || j.Status == DriverJobStatus.Pending), ct)
                  ?? throw new NotFoundException("This job is no longer available.");
        return job.ToDetail(driver.Id, Market.DriverEarningPerJob);
    }

    /// <summary>Resolves a notification's order to a job: the caller's own job first, then one still on the marketplace.</summary>
    public async Task<DriverJobDetailDto> GetJobForOrderAsync(Guid orderId, CancellationToken ct)
    {
        var driver = await profile.DriverAsync(ct);
        var job = await JobQuery()
                      .Where(j => j.OrderId == orderId && j.DriverId == driver.Id)
                      .OrderByDescending(j => j.CreatedAt)
                      .FirstOrDefaultAsync(ct)
                  ?? await JobQuery()
                      .Where(j => j.OrderId == orderId && j.Status == DriverJobStatus.Pending)
                      .OrderByDescending(j => j.CreatedAt)
                      .FirstOrDefaultAsync(ct)
                  ?? throw new NotFoundException("This job is no longer available.");
        return job.ToDetail(driver.Id, Market.DriverEarningPerJob);
    }

    // ── D13 Earnings ─────────────────────────────────────────────────────────

    public async Task<DriverEarningsDto> GetEarningsAsync(EarningsPeriod period, CancellationToken ct)
    {
        var driver = await profile.DriverAsync(ct);
        var today = DateTime.UtcNow.Date;
        var from = period == EarningsPeriod.Week ? today.AddDays(-6) : today.AddDays(-27);
        var to = today.AddDays(1);

        var jobs = await JobQuery()
            .Where(j => j.DriverId == driver.Id && j.Status == DriverJobStatus.Completed && j.CompletedAt >= from && j.CompletedAt < to)
            .OrderByDescending(j => j.CompletedAt)
            .ToListAsync(ct);

        var rate = Market.DriverEarningPerJob;
        var bucketDays = period == EarningsPeriod.Week ? 1 : 7;
        var buckets = period == EarningsPeriod.Week ? 7 : 4;
        var bars = Enumerable.Range(0, buckets).Select(i =>
        {
            var start = from.AddDays(i * bucketDays);
            var count = jobs.Count(j => j.CompletedAt >= start && j.CompletedAt < start.AddDays(bucketDays));
            var label = period == EarningsPeriod.Week ? start.ToString("ddd") : start.ToString("d MMM");
            return new EarningsBarDto { Label = label, Start = start, Amount = count * rate, Jobs = count };
        }).ToList();

        return new DriverEarningsDto
        {
            Period = period,
            From = from,
            To = today,
            Total = jobs.Count * rate,
            JobCount = jobs.Count,
            PickupCount = jobs.Count(j => j.JobType == DriverJobType.Pickup),
            DeliveryCount = jobs.Count(j => j.JobType == DriverJobType.Delivery),
            RatePerJob = rate,
            AveragePerJob = jobs.Count == 0 ? 0 : rate,
            Currency = Market.Currency,
            Bars = bars,
            Jobs = jobs.Select(j => j.ToDto(j.Order, rate)).ToList()
        };
    }

    // ── D03/D04 Accept ───────────────────────────────────────────────────────

    public async Task<DriverJobDetailDto> AcceptAsync(Guid jobId, CancellationToken ct)
    {
        var driver = await profile.DriverAsync(ct);
        if (!driver.IsActive)
            throw new ForbiddenException(driver.ApprovedAt is null
                ? "Your driver account is still under review. You can accept jobs once an admin approves it."
                : "Your driver account has been deactivated. Please contact support.");
        if (!driver.IsAvailable)
            throw new ConflictException("Go online to accept jobs.");
        if (await db.DriverJobs.AnyAsync(j => j.DriverId == driver.Id && (j.Status == DriverJobStatus.Assigned || j.Status == DriverJobStatus.InProgress), ct))
            throw new ConflictException("Finish your active job before accepting another.");

        var job = await JobQuery().FirstOrDefaultAsync(j => j.Id == jobId, ct) ?? throw new NotFoundException("Job not found.");
        if (job.Status != DriverJobStatus.Pending)
            throw new ConflictException("This job has already been taken.");
        EnsureNotOnHold(job.Order);

        job.DriverId = driver.Id;
        job.Status = DriverJobStatus.Assigned;
        job.AssignedAt = DateTime.UtcNow;

        var driverName = (await db.Users.FirstAsync(u => u.Id == driver.UserId, ct)).Name;
        var order = job.Order;
        if (job.JobType == DriverJobType.Pickup)
        {
            order.ChangeStatus(OrderStatus.PickupAssigned, driver.UserId, $"Pickup assigned to {driverName}");
            db.Notify(order.Customer.UserId, order, "Driver assigned", $"{driverName} will collect {order.OrderNumber}. Have your pickup code ready.");
        }
        else
        {
            order.ChangeStatus(OrderStatus.DeliveryAssigned, driver.UserId, $"Delivery assigned to {driverName}");
            db.Notify(order.Customer.UserId, order, "Delivery driver assigned", $"{driverName} will deliver {order.OrderNumber}.");
        }
        db.Notify(order.ServiceProvider.UserId, order, "Driver assigned", $"{driverName} accepted the {job.JobType.ToString().ToLowerInvariant()} job for {order.OrderNumber}.");

        await SaveWithConcurrencyGuardAsync(ct);
        return job.ToDetail(driver.Id, Market.DriverEarningPerJob);
    }

    /// <summary>Driver drops an accepted job before collecting items – it goes back to the marketplace.</summary>
    public async Task<DriverJobDetailDto> ReleaseAsync(Guid jobId, CancellationToken ct)
    {
        var (driver, job) = await LoadMyJobAsync(jobId, ct);
        if (job.Status != DriverJobStatus.Assigned)
            throw new ConflictException("Only jobs that haven't started can be released.");

        job.DriverId = null;
        job.Driver = null;
        job.Status = DriverJobStatus.Pending;
        job.AssignedAt = null;
        var order = job.Order;
        order.ChangeStatus(job.JobType == DriverJobType.Pickup ? OrderStatus.Confirmed : OrderStatus.ReadyForDelivery,
            driver.UserId, "Driver released the job – back on the marketplace");
        db.Notify(order.ServiceProvider.UserId, order, "Driver released job",
            $"The driver released the {job.JobType.ToString().ToLowerInvariant()} job for {order.OrderNumber}. It's back on the marketplace.");

        await db.SaveChangesAsync(ct);
        return job.ToDetail(driver.Id, Market.DriverEarningPerJob);
    }

    // ── D05/D06 Pickup from customer (item check + pickup code) ──────────────

    public async Task<DriverJobDetailDto> ConfirmPickupAsync(Guid jobId, DriverPickupRequest request, CancellationToken ct)
    {
        var (driver, job) = await LoadMyJobAsync(jobId, ct);
        if (job.JobType != DriverJobType.Pickup || job.Status != DriverJobStatus.Assigned)
            throw new ConflictException("This job is not waiting for a customer pickup.");

        var order = job.Order;
        var verification = await db.OrderVerifications.FirstOrDefaultAsync(v => v.OrderId == order.Id && v.Type == VerificationType.QR && v.VerifiedAt == null, ct);
        if (verification is null || verification.OtpCode != request.Code.Trim())
            throw new BadRequestException("The pickup code doesn't match. Ask the customer to show the code in their app.");

        // Item check (D05): every line must belong to this order.
        IReadOnlyList<DriverItemCheckRequest> checks = request.Items ?? Array.Empty<DriverItemCheckRequest>();
        var lines = order.Items.ToDictionary(i => i.Id);
        var unknown = checks.FirstOrDefault(c => !lines.ContainsKey(c.OrderItemId));
        if (unknown is not null)
            throw new BadRequestException($"Order item {unknown.OrderItemId} is not part of {order.OrderNumber}.");

        var now = DateTime.UtcNow;
        verification.VerifiedAt = now;
        verification.VerifiedByUserId = driver.UserId;
        if (checks.Count > 0)
            db.OrderVerifications.Add(new OrderVerification { OrderId = order.Id, Type = VerificationType.Item, VerifiedAt = now, VerifiedByUserId = driver.UserId });
        job.Status = DriverJobStatus.InProgress;

        var expected = order.Items.Sum(i => i.Quantity);
        var counted = checks.Count > 0 ? checks.Sum(c => c.CountedQuantity) : request.ItemCount;
        var issues = checks
            .Where(c => c.Condition != ItemCondition.Good || c.CountedQuantity != lines[c.OrderItemId].Quantity)
            .Select(c =>
            {
                var line = lines[c.OrderItemId];
                var text = $"{line.Product.Name} ({line.ServiceItem.Name}): {c.Condition}";
                if (c.CountedQuantity != line.Quantity)
                    text += $" ({c.CountedQuantity}/{line.Quantity} pcs)";
                if (!string.IsNullOrWhiteSpace(c.Note))
                    text += $" – {c.Note.Trim()}";
                return text;
            })
            .ToList();

        var notes = new List<string> { counted is null ? "Items collected from customer" : $"Collected {counted} of {expected} items from customer" };
        notes.AddRange(issues);
        if (!string.IsNullOrWhiteSpace(request.Notes))
            notes.Add(request.Notes.Trim());
        order.ChangeStatus(OrderStatus.PickedUp, driver.UserId, Truncate(string.Join(". ", notes), 500));

        db.Notify(order.ServiceProvider.UserId, order, "Driver on the way", $"{order.OrderNumber} has been collected and is on its way to you.");
        if (issues.Count > 0)
            db.Notify(order.ServiceProvider.UserId, order, "Pickup notes",
                Truncate($"The driver noted {issues.Count} issue{(issues.Count == 1 ? "" : "s")} on {order.OrderNumber}: {string.Join("; ", issues)}", 500),
                NotificationType.System);
        db.Notify(order.Customer.UserId, order, "Items picked up", $"Your items for {order.OrderNumber} are on the way to {order.ServiceProvider.BusinessName}.");
        await db.SaveChangesAsync(ct);
        return job.ToDetail(driver.Id, Market.DriverEarningPerJob);
    }

    // ── D08 Hand over to provider ────────────────────────────────────────────

    public async Task<DriverJobDetailDto> ConfirmHandoverAsync(Guid jobId, VerifyCodeRequest request, CancellationToken ct)
    {
        var (driver, job) = await LoadMyJobAsync(jobId, ct);
        if (job.JobType != DriverJobType.Pickup || job.Status != DriverJobStatus.InProgress)
            throw new ConflictException("This job is not ready for handover.");

        job.Status = DriverJobStatus.Completed;
        job.CompletedAt = DateTime.UtcNow;
        db.OrderStatusHistory.Add(new OrderStatusHistory
        {
            OrderId = job.OrderId,
            Status = job.Order.Status,
            ChangedByUserId = driver.UserId,
            Notes = string.IsNullOrWhiteSpace(request.Notes) ? "Delivered to provider" : Truncate($"Delivered to provider. {request.Notes.Trim()}", 500)
        });

        db.Notify(job.Order.ServiceProvider.UserId, job.Order, "Items delivered", $"{job.Order.OrderNumber} was handed over. Please confirm receipt.");
        await db.SaveChangesAsync(ct);
        return job.ToDetail(driver.Id, Market.DriverEarningPerJob);
    }

    // ── D09/D10 Collect from provider ────────────────────────────────────────

    public async Task<DriverJobDetailDto> StartDeliveryAsync(Guid jobId, StartDeliveryRequest? request, CancellationToken ct)
    {
        var (driver, job) = await LoadMyJobAsync(jobId, ct);
        if (job.JobType != DriverJobType.Delivery || job.Status != DriverJobStatus.Assigned)
            throw new ConflictException("This job is not waiting for collection from the provider.");

        job.Status = DriverJobStatus.InProgress;
        var notes = request?.PackageCount is { } packages
            ? $"Collected {packages} package{(packages == 1 ? "" : "s")} from provider · out for delivery"
            : "Collected from provider · out for delivery";
        if (!string.IsNullOrWhiteSpace(request?.Notes))
            notes += $". {request.Notes.Trim()}";
        job.Order.ChangeStatus(OrderStatus.OutForDelivery, driver.UserId, Truncate(notes, 500));
        db.Notify(job.Order.Customer.UserId, job.Order, "Out for delivery", $"{job.Order.OrderNumber} is on its way. Have your delivery OTP ready.");
        await db.SaveChangesAsync(ct);
        return job.ToDetail(driver.Id, Market.DriverEarningPerJob);
    }

    // ── D11 Deliver to customer (OTP) ────────────────────────────────────────

    public async Task<DriverJobDetailDto> ConfirmDeliveryAsync(Guid jobId, VerifyCodeRequest request, CancellationToken ct)
    {
        var (driver, job) = await LoadMyJobAsync(jobId, ct);
        if (job.JobType != DriverJobType.Delivery || job.Status != DriverJobStatus.InProgress)
            throw new ConflictException("This job is not out for delivery.");

        var order = job.Order;
        var otp = await db.OrderVerifications.FirstOrDefaultAsync(v => v.OrderId == order.Id && v.Type == VerificationType.OTP && v.VerifiedAt == null, ct);
        if (otp is null || otp.OtpCode != request.Code.Trim())
            throw new BadRequestException("Incorrect OTP. Ask the customer for the delivery code in their app.");

        otp.VerifiedAt = DateTime.UtcNow;
        otp.VerifiedByUserId = order.Customer.UserId;
        job.Status = DriverJobStatus.Completed;
        job.CompletedAt = DateTime.UtcNow;

        order.ChangeStatus(OrderStatus.Delivered, driver.UserId, "Delivered to customer · OTP verified");
        order.ChangeStatus(OrderStatus.Completed, driver.UserId, "Order completed");

        db.Notify(order.Customer.UserId, order, "Delivered", $"{order.OrderNumber} has been delivered. Thank you for using E Dry Cleaners!");
        db.Notify(order.ServiceProvider.UserId, order, "Order completed", $"{order.OrderNumber} was delivered to the customer.");
        await db.SaveChangesAsync(ct);
        return job.ToDetail(driver.Id, Market.DriverEarningPerJob);
    }

    // ── Helpers ──────────────────────────────────────────────────────────────

    private IQueryable<DriverJob> JobQuery() => db.DriverJobs
        .Include(j => j.Order).ThenInclude(o => o.Customer).ThenInclude(c => c.User)
        .Include(j => j.Order).ThenInclude(o => o.ServiceProvider)
        .Include(j => j.Order).ThenInclude(o => o.Items).ThenInclude(i => i.Product).ThenInclude(p => p.Category)
        .Include(j => j.Order).ThenInclude(o => o.Items).ThenInclude(i => i.ServiceItem)
        .Include(j => j.Driver!).ThenInclude(d => d.User)
        .Include(j => j.PickupAddress)
        .Include(j => j.DeliveryAddress)
        .AsSplitQuery();

    private IQueryable<DriverJob> AvailableQuery() => JobQuery().Where(j =>
        j.Status == DriverJobStatus.Pending &&
        j.Order.Status != OrderStatus.Cancelled &&
        j.Order.Status != OrderStatus.Disputed);

    private async Task<(Driver Driver, DriverJob Job)> LoadMyJobAsync(Guid jobId, CancellationToken ct)
    {
        var driver = await profile.DriverAsync(ct);
        var job = await JobQuery().FirstOrDefaultAsync(j => j.Id == jobId && j.DriverId == driver.Id, ct)
                  ?? throw new NotFoundException("Job not found.");
        EnsureNotOnHold(job.Order);
        return (driver, job);
    }

    private static void EnsureNotOnHold(Order order)
    {
        if (order.Status == OrderStatus.Disputed)
            throw new ConflictException("This order is on hold due to a dispute.");
        if (order.Status == OrderStatus.Cancelled)
            throw new ConflictException("This order was cancelled.");
    }

    private static string Truncate(string text, int max) => text.Length <= max ? text : text[..(max - 1)] + "…";

    private async Task SaveWithConcurrencyGuardAsync(CancellationToken ct)
    {
        try
        {
            await db.SaveChangesAsync(ct);
        }
        catch (DbUpdateConcurrencyException)
        {
            throw new ConflictException("This job has just been taken by another driver.");
        }
    }
}
