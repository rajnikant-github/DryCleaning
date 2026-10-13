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
        return new DriverProfileDto(driver.Id, user.ToDto(driver.Id), driver.LicenseNumber, driver.IsAvailable, driver.IsActive, driver.Address?.ToDto());
    }

    public async Task<DriverProfileDto> UpdateProfileAsync(UpdateDriverProfileRequest request, CancellationToken ct)
    {
        var driver = await profile.DriverAsync(ct);
        driver.LicenseNumber = request.LicenseNumber.Trim();
        await db.SaveChangesAsync(ct);
        return await GetProfileAsync(ct);
    }

    public async Task<DriverDashboardDto> GetDashboardAsync(CancellationToken ct)
    {
        var driver = await profile.DriverAsync(ct);
        var available = driver.IsAvailable ? await AvailableQuery().CountAsync(ct) : 0;
        var active = await JobQuery().Where(j => j.DriverId == driver.Id && (j.Status == DriverJobStatus.Assigned || j.Status == DriverJobStatus.InProgress))
            .OrderBy(j => j.AssignedAt).FirstOrDefaultAsync(ct);
        var today = DateTime.UtcNow.Date;
        var completedToday = await db.DriverJobs.CountAsync(j => j.DriverId == driver.Id && j.Status == DriverJobStatus.Completed && j.CompletedAt >= today, ct);

        return new DriverDashboardDto(driver.IsAvailable, available, active?.ToDto(active.Order, Market.DriverEarningPerJob),
            completedToday, completedToday * Market.DriverEarningPerJob, Market.Currency);
    }

    public async Task<bool> SetAvailabilityAsync(bool isAvailable, CancellationToken ct)
    {
        var driver = await profile.DriverAsync(ct);
        if (!driver.IsActive)
            throw new ForbiddenException("Your driver account is not active.");
        driver.IsAvailable = isAvailable;
        await db.SaveChangesAsync(ct);
        return driver.IsAvailable;
    }

    public async Task<PagedResult<DriverJobDto>> GetAvailableJobsAsync(PagedRequest request, CancellationToken ct)
    {
        var driver = await profile.DriverAsync(ct);
        if (!driver.IsAvailable)
            return new PagedResult<DriverJobDto>([], request.Page, request.PageSize, 0);

        var (items, total) = await AvailableQuery().OrderBy(j => j.CreatedAt).PageAsync(request, ct);
        return new PagedResult<DriverJobDto>(items.Select(j => j.ToDto(j.Order, Market.DriverEarningPerJob)).ToList(), request.Page, request.PageSize, total);
    }

    public async Task<PagedResult<DriverJobDto>> GetMyJobsAsync(DriverJobsQuery query, CancellationToken ct)
    {
        var driver = await profile.DriverAsync(ct);
        var jobs = JobQuery().Where(j => j.DriverId == driver.Id);
        if (query.Status is not null)
            jobs = jobs.Where(j => j.Status == query.Status);

        var (items, total) = await jobs.OrderByDescending(j => j.AssignedAt).PageAsync(query, ct);
        return new PagedResult<DriverJobDto>(items.Select(j => j.ToDto(j.Order, Market.DriverEarningPerJob)).ToList(), query.Page, query.PageSize, total);
    }

    public async Task<DriverJobDto> GetJobAsync(Guid jobId, CancellationToken ct)
    {
        var driver = await profile.DriverAsync(ct);
        var job = await JobQuery().FirstOrDefaultAsync(j => j.Id == jobId && (j.DriverId == driver.Id || j.Status == DriverJobStatus.Pending), ct)
                  ?? throw new NotFoundException("Job not found.");
        return job.ToDto(job.Order, Market.DriverEarningPerJob);
    }

    // ── D03/D04 Accept ───────────────────────────────────────────────────────

    public async Task<DriverJobDto> AcceptAsync(Guid jobId, CancellationToken ct)
    {
        var driver = await profile.DriverAsync(ct);
        if (!driver.IsAvailable)
            throw new ConflictException("Go online to accept jobs.");
        if (await db.DriverJobs.AnyAsync(j => j.DriverId == driver.Id && (j.Status == DriverJobStatus.Assigned || j.Status == DriverJobStatus.InProgress), ct))
            throw new ConflictException("Finish your active job before accepting another.");

        var job = await JobQuery().FirstOrDefaultAsync(j => j.Id == jobId, ct) ?? throw new NotFoundException("Job not found.");
        if (job.Status != DriverJobStatus.Pending)
            throw new ConflictException("This job has already been taken.");

        job.DriverId = driver.Id;
        job.Status = DriverJobStatus.Assigned;
        job.AssignedAt = DateTime.UtcNow;

        var driverName = (await db.Users.FirstAsync(u => u.Id == driver.UserId, ct)).Name;
        var order = job.Order;
        if (job.JobType == DriverJobType.Pickup)
        {
            order.ChangeStatus(OrderStatus.PickupAssigned, driver.UserId, $"Pickup assigned to {driverName}");
            db.Notify(order.Customer.UserId, order, "Driver assigned", $"{driverName} will collect {order.OrderNumber}.");
        }
        else
        {
            order.ChangeStatus(OrderStatus.DeliveryAssigned, driver.UserId, $"Delivery assigned to {driverName}");
            db.Notify(order.Customer.UserId, order, "Delivery driver assigned", $"{driverName} will deliver {order.OrderNumber}.");
        }
        db.Notify(order.ServiceProvider.UserId, order, "Driver assigned", $"{driverName} accepted the {job.JobType.ToString().ToLowerInvariant()} job for {order.OrderNumber}.");

        await SaveWithConcurrencyGuardAsync(ct);
        return await GetJobAsync(jobId, ct);
    }

    /// <summary>Driver drops an accepted job before collecting items – it goes back to the marketplace.</summary>
    public async Task<DriverJobDto> ReleaseAsync(Guid jobId, CancellationToken ct)
    {
        var (driver, job) = await LoadMyJobAsync(jobId, ct);
        if (job.Status != DriverJobStatus.Assigned)
            throw new ConflictException("Only jobs that haven't started can be released.");

        job.DriverId = null;
        job.Status = DriverJobStatus.Pending;
        job.AssignedAt = null;
        var order = job.Order;
        order.ChangeStatus(job.JobType == DriverJobType.Pickup ? OrderStatus.Confirmed : OrderStatus.ReadyForDelivery,
            driver.UserId, "Driver released the job – back on the marketplace");

        await db.SaveChangesAsync(ct);
        return job.ToDto(order, Market.DriverEarningPerJob);
    }

    // ── D06 Pickup from customer (verify pickup code) ────────────────────────

    public async Task<DriverJobDto> ConfirmPickupAsync(Guid jobId, VerifyCodeRequest request, CancellationToken ct)
    {
        var (driver, job) = await LoadMyJobAsync(jobId, ct);
        if (job.JobType != DriverJobType.Pickup || job.Status != DriverJobStatus.Assigned)
            throw new ConflictException("This job is not waiting for a customer pickup.");

        var order = job.Order;
        var verification = await db.OrderVerifications.FirstOrDefaultAsync(v => v.OrderId == order.Id && v.Type == VerificationType.QR && v.VerifiedAt == null, ct);
        if (verification is null || verification.OtpCode != request.Code.Trim())
            throw new BadRequestException("The pickup code doesn't match. Ask the customer to show the code in their app.");

        verification.VerifiedAt = DateTime.UtcNow;
        verification.VerifiedByUserId = driver.UserId;
        job.Status = DriverJobStatus.InProgress;

        var notes = request.ItemCount is null ? "Items collected from customer" : $"Collected {request.ItemCount} items from customer";
        if (!string.IsNullOrWhiteSpace(request.Notes))
            notes += $". {request.Notes.Trim()}";
        order.ChangeStatus(OrderStatus.PickedUp, driver.UserId, notes);

        db.Notify(order.ServiceProvider.UserId, order, "Driver on the way", $"{order.OrderNumber} has been collected and is on its way to you.");
        db.Notify(order.Customer.UserId, order, "Items picked up", $"Your items for {order.OrderNumber} are on the way to {order.ServiceProvider.BusinessName}.");
        await db.SaveChangesAsync(ct);
        return job.ToDto(order, Market.DriverEarningPerJob);
    }

    // ── D08 Hand over to provider ────────────────────────────────────────────

    public async Task<DriverJobDto> ConfirmHandoverAsync(Guid jobId, VerifyCodeRequest request, CancellationToken ct)
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
            Notes = string.IsNullOrWhiteSpace(request.Notes) ? "Delivered to provider" : $"Delivered to provider. {request.Notes.Trim()}"
        });

        db.Notify(job.Order.ServiceProvider.UserId, job.Order, "Items delivered", $"{job.Order.OrderNumber} was handed over. Please confirm receipt.");
        await db.SaveChangesAsync(ct);
        return job.ToDto(job.Order, Market.DriverEarningPerJob);
    }

    // ── D09/D10 Collect from provider ────────────────────────────────────────

    public async Task<DriverJobDto> StartDeliveryAsync(Guid jobId, CancellationToken ct)
    {
        var (driver, job) = await LoadMyJobAsync(jobId, ct);
        if (job.JobType != DriverJobType.Delivery || job.Status != DriverJobStatus.Assigned)
            throw new ConflictException("This job is not waiting for collection from the provider.");

        job.Status = DriverJobStatus.InProgress;
        job.Order.ChangeStatus(OrderStatus.OutForDelivery, driver.UserId, "Collected from provider · out for delivery");
        db.Notify(job.Order.Customer.UserId, job.Order, "Out for delivery", $"{job.Order.OrderNumber} is on its way. Have your delivery OTP ready.");
        await db.SaveChangesAsync(ct);
        return job.ToDto(job.Order, Market.DriverEarningPerJob);
    }

    // ── D11 Deliver to customer (OTP) ────────────────────────────────────────

    public async Task<DriverJobDto> ConfirmDeliveryAsync(Guid jobId, VerifyCodeRequest request, CancellationToken ct)
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

        db.Notify(order.Customer.UserId, order, "Delivered", $"{order.OrderNumber} has been delivered. Thank you for using EDry Cleaners!");
        db.Notify(order.ServiceProvider.UserId, order, "Order completed", $"{order.OrderNumber} was delivered to the customer.");
        await db.SaveChangesAsync(ct);
        return job.ToDto(order, Market.DriverEarningPerJob);
    }

    // ── Helpers ──────────────────────────────────────────────────────────────

    private IQueryable<DriverJob> JobQuery() => db.DriverJobs
        .Include(j => j.Order).ThenInclude(o => o.Customer).ThenInclude(c => c.User)
        .Include(j => j.Order).ThenInclude(o => o.ServiceProvider)
        .Include(j => j.Order).ThenInclude(o => o.Items)
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
        if (job.Order.Status == OrderStatus.Disputed)
            throw new ConflictException("This order is on hold due to a dispute.");
        return (driver, job);
    }

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
