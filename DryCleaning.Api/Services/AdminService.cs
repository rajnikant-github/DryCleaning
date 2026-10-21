using DryCleaning.Api.Common;
using DryCleaning.Api.Data;
using DryCleaning.Api.Domain;
using DryCleaning.Api.Dtos;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace DryCleaning.Api.Services;

/// <summary>Admin web portal (A02–A12).</summary>
public sealed class AdminService(AppDbContext db, CurrentProfile profile, IOptions<MarketplaceOptions> marketplace)
{
    private MarketplaceOptions Market => marketplace.Value;

    // ── Users (any role) ─────────────────────────────────────────────────────

    public async Task<PagedResult<AdminUserDto>> GetUsersAsync(PagedRequest request, UserRole? role, CancellationToken ct)
    {
        var query = db.Users.AsNoTracking().AsQueryable();
        if (role is not null)
            query = query.Where(u => u.Role == role);
        if (!string.IsNullOrWhiteSpace(request.Search))
            query = query.Where(u => u.Name.Contains(request.Search) || (u.Email ?? "").Contains(request.Search) || (u.Phone ?? "").Contains(request.Search));

        var total = await query.CountAsync(ct);
        var items = await query.OrderBy(u => u.Name)
            .Skip((request.Page - 1) * request.PageSize).Take(request.PageSize)
            .Select(u => new AdminUserDto { Id = u.Id, Name = u.Name, Role = u.Role, Email = u.Email, Phone = u.Phone, IsActive = u.IsActive })
            .ToListAsync(ct);
        return new PagedResult<AdminUserDto> { Items = items, Page = request.Page, PageSize = request.PageSize, TotalCount = total };
    }

    // ── A02 Dashboard ────────────────────────────────────────────────────────

    public async Task<AdminDashboardDto> GetDashboardAsync(CancellationToken ct)
    {
        var byStatus = await db.Orders.GroupBy(o => o.Status).Select(g => new { g.Key, Count = g.Count() }).ToListAsync(ct);
        var since = DateTime.UtcNow.Date.AddDays(-29);

        var daily = await db.Orders
            .Where(o => o.CreatedAt >= since)
            .GroupBy(o => o.CreatedAt.Date)
            .Select(g => new { Date = g.Key, Orders = g.Count(), Revenue = g.Where(o => o.Status == OrderStatus.Completed).Sum(o => o.TotalAmount) })
            .ToListAsync(ct);

        var last30 = Enumerable.Range(0, 30)
            .Select(i => since.AddDays(i))
            .Select(d =>
            {
                var row = daily.FirstOrDefault(x => x.Date == d);
                return new DailyCountDto
                {
                    Date = DateOnly.FromDateTime(d),
                    Orders = row?.Orders ?? 0,
                    Revenue = row?.Revenue ?? 0
                };
            })
            .ToList();

        var revenue = await db.Payments.Where(p => p.Status == PaymentStatus.Success).SumAsync(p => (decimal?)p.Amount, ct) ?? 0;
        int Count(params OrderStatus[] statuses) => byStatus.Where(x => statuses.Contains(x.Key)).Sum(x => x.Count);

        return new AdminDashboardDto
        {
            TotalCustomers = await db.Customers.CountAsync(ct),
            TotalProviders = await db.ServiceProviders.CountAsync(ct),
            TotalDrivers = await db.Drivers.CountAsync(ct),
            ActiveOrders = byStatus.Where(x => x.Key is not (OrderStatus.Completed or OrderStatus.Cancelled or OrderStatus.Delivered)).Sum(x => x.Count),
            CompletedOrders = Count(OrderStatus.Completed, OrderStatus.Delivered),
            Revenue = revenue,
            OpenDisputes = await db.Disputes.CountAsync(d => d.Status == DisputeStatus.Open || d.Status == DisputeStatus.UnderReview, ct),
            Currency = Market.Currency,
            OrdersByStatus = byStatus.ToDictionary(x => x.Key.ToString(), x => x.Count),
            OrdersLast30Days = last30
        };
    }

    // ── A03 Customers ────────────────────────────────────────────────────────

    public async Task<PagedResult<AdminCustomerDto>> GetCustomersAsync(PagedRequest request, CancellationToken ct)
    {
        var query = db.Customers.Include(c => c.User).Include(c => c.Address).AsQueryable();
        if (!string.IsNullOrWhiteSpace(request.Search))
            query = query.Where(c => c.User.Name.Contains(request.Search) || (c.User.Email ?? "").Contains(request.Search) || (c.User.Phone ?? "").Contains(request.Search));

        var total = await query.CountAsync(ct);
        var items = await query.OrderByDescending(c => c.CreatedAt)
            .Skip((request.Page - 1) * request.PageSize).Take(request.PageSize)
            .Select(c => new AdminCustomerDto
            {
                Id = c.Id,
                UserId = c.UserId,
                Name = c.User.Name,
                Phone = c.User.Phone,
                Email = c.User.Email,
                Address = c.Address == null ? null : c.Address.FormattedAddress,
                OrderCount = c.Orders.Count,
                IsActive = c.User.IsActive,
                CreatedAt = c.CreatedAt
            })
            .ToListAsync(ct);
        return new PagedResult<AdminCustomerDto>
        {
            Items = items,
            Page = request.Page,
            PageSize = request.PageSize,
            TotalCount = total
        };
    }

    // ── A04 Providers ────────────────────────────────────────────────────────

    public async Task<PagedResult<AdminProviderDto>> GetProvidersAsync(PagedRequest request, bool? isActive, CancellationToken ct)
    {
        var query = db.ServiceProviders.AsQueryable();
        if (isActive is not null)
            query = query.Where(p => p.IsActive == isActive);
        if (!string.IsNullOrWhiteSpace(request.Search))
            query = query.Where(p => p.BusinessName.Contains(request.Search) || p.User.Name.Contains(request.Search));

        var total = await query.CountAsync(ct);
        var items = await query.OrderByDescending(p => p.CreatedAt)
            .Skip((request.Page - 1) * request.PageSize).Take(request.PageSize)
            .Select(p => new AdminProviderDto
            {
                Id = p.Id,
                UserId = p.UserId,
                OwnerName = p.User.Name,
                BusinessName = p.BusinessName,
                Categories = p.Categories.Where(c => c.IsActive).Select(c => c.Category.Name).ToList(),
                OrderCount = p.Orders.Count,
                IsActive = p.IsActive,
                CreatedAt = p.CreatedAt
            })
            .ToListAsync(ct);
        return new PagedResult<AdminProviderDto>
        {
            Items = items,
            Page = request.Page,
            PageSize = request.PageSize,
            TotalCount = total
        };
    }

    /// <summary>Approve (true) or deactivate (false) a provider.</summary>
    public async Task SetProviderActiveAsync(Guid providerId, bool isActive, CancellationToken ct)
    {
        var provider = await db.ServiceProviders.FirstOrDefaultAsync(p => p.Id == providerId, ct)
                       ?? throw new NotFoundException("Service provider not found.");
        provider.IsActive = isActive;
        db.Notify(provider.UserId, null,
            isActive ? "Account approved" : "Account deactivated",
            isActive ? $"{provider.BusinessName} is now live on E Dry Cleaners." : $"{provider.BusinessName} has been deactivated. Contact support for help.",
            NotificationType.System);
        await db.SaveChangesAsync(ct);
    }

    public async Task<ProviderCatalogDto> GetProviderCatalogAsync(Guid providerId, CancellationToken ct)
    {
        var provider = await db.ServiceProviders
            .Include(p => p.Categories).ThenInclude(c => c.Category)
            .Include(p => p.Products).ThenInclude(p => p.ServiceItems)
            .Include(p => p.Products).ThenInclude(p => p.Category)
            .AsSplitQuery()
            .FirstOrDefaultAsync(p => p.Id == providerId, ct)
            ?? throw new NotFoundException("Service provider not found.");

        var categories = provider.Categories
            .OrderBy(c => c.Category.DisplayOrder)
            .Select(c => new CatalogCategoryDto
            {
                Id = c.CategoryId,
                Name = c.Category.Name,
                Description = c.Category.Description,
                ImageUrl = c.Category.ImageUrl,
                Products = provider.Products.Where(p => p.CategoryId == c.CategoryId).OrderBy(p => p.DisplayOrder).Select(p => p.ToDto()).ToList()
            })
            .ToList();
        return new ProviderCatalogDto
        {
            ProviderId = provider.Id,
            BusinessName = provider.BusinessName,
            Currency = Market.Currency,
            Categories = categories
        };
    }

    // ── A05 Drivers (no ServiceProviderId) ───────────────────────────────────

    public async Task<PagedResult<AdminDriverDto>> GetDriversAsync(PagedRequest request, CancellationToken ct)
    {
        var query = db.Drivers.AsQueryable();
        if (!string.IsNullOrWhiteSpace(request.Search))
            query = query.Where(d => d.User.Name.Contains(request.Search) || (d.User.Phone ?? "").Contains(request.Search) || d.LicenseNumber.Contains(request.Search));

        var total = await query.CountAsync(ct);
        var items = await query.OrderBy(d => d.User.Name)
            .Skip((request.Page - 1) * request.PageSize).Take(request.PageSize)
            .Select(d => new AdminDriverDto
            {
                Id = d.Id,
                UserId = d.UserId,
                Name = d.User.Name,
                Phone = d.User.Phone,
                LicenseNumber = d.LicenseNumber,
                IsAvailable = d.IsAvailable,
                ActiveJobs = d.Jobs.Count(j => j.Status == DriverJobStatus.Assigned || j.Status == DriverJobStatus.InProgress),
                CompletedJobs = d.Jobs.Count(j => j.Status == DriverJobStatus.Completed),
                IsActive = d.IsActive && d.User.IsActive
            })
            .ToListAsync(ct);
        return new PagedResult<AdminDriverDto>
        {
            Items = items,
            Page = request.Page,
            PageSize = request.PageSize,
            TotalCount = total
        };
    }

    public async Task SetDriverActiveAsync(Guid driverId, bool isActive, CancellationToken ct)
    {
        var driver = await db.Drivers.FirstOrDefaultAsync(d => d.Id == driverId, ct) ?? throw new NotFoundException("Driver not found.");
        driver.IsActive = isActive;
        if (!isActive)
            driver.IsAvailable = false;
        await db.SaveChangesAsync(ct);
    }

    /// <summary>Deactivate / reactivate any user account (customer, provider, driver).</summary>
    public async Task SetUserActiveAsync(Guid userId, bool isActive, CancellationToken ct)
    {
        var user = await db.Users.FirstOrDefaultAsync(u => u.Id == userId, ct) ?? throw new NotFoundException("User not found.");
        if (user.Id == profile.UserId)
            throw new BadRequestException("You can't deactivate your own account.");
        user.IsActive = isActive;
        await db.SaveChangesAsync(ct);
    }

    // ── A08/A09 Orders ───────────────────────────────────────────────────────

    public async Task<PagedResult<AdminOrderDto>> GetOrdersAsync(AdminOrdersQuery query, CancellationToken ct)
    {
        var orders = db.Orders.AsQueryable();
        if (query.Status is not null)
            orders = orders.Where(o => o.Status == query.Status);
        if (!string.IsNullOrWhiteSpace(query.Search))
            orders = orders.Where(o => o.OrderNumber.Contains(query.Search) || o.Customer.User.Name.Contains(query.Search) || o.ServiceProvider.BusinessName.Contains(query.Search));

        var total = await orders.CountAsync(ct);
        var items = await orders.OrderByDescending(o => o.CreatedAt)
            .Skip((query.Page - 1) * query.PageSize).Take(query.PageSize)
            .Select(o => new AdminOrderDto
            {
                Id = o.Id,
                OrderNumber = o.OrderNumber,
                CustomerName = o.Customer.User.Name,
                ProviderName = o.ServiceProvider.BusinessName,
                DriverName = o.DriverJobs.Where(j => j.Driver != null && j.Status != DriverJobStatus.Cancelled)
                    .OrderByDescending(j => j.AssignedAt).Select(j => j.Driver!.User.Name).FirstOrDefault(),
                Status = o.Status,
                TotalAmount = o.TotalAmount,
                CreatedAt = o.CreatedAt
            })
            .ToListAsync(ct);
        return new PagedResult<AdminOrderDto> { Items = items, Page = query.Page, PageSize = query.PageSize, TotalCount = total };
    }

    public async Task<OrderDetailDto> GetOrderAsync(Guid orderId, CancellationToken ct)
    {
        var order = await db.Orders.WithDetailIncludes().FirstOrDefaultAsync(o => o.Id == orderId, ct)
                    ?? throw new NotFoundException("Order not found.");
        return order.ToDetail(Market.Currency, Market.DriverEarningPerJob, includeCustomerCodes: false);
    }

    // ── A10 Driver jobs ──────────────────────────────────────────────────────

    public async Task<PagedResult<DriverJobDto>> GetDriverJobsAsync(AdminJobsQuery query, CancellationToken ct)
    {
        var jobs = db.DriverJobs
            .Include(j => j.Order).ThenInclude(o => o.Customer).ThenInclude(c => c.User)
            .Include(j => j.Order).ThenInclude(o => o.ServiceProvider)
            .Include(j => j.Order).ThenInclude(o => o.Items)
            .Include(j => j.Driver!).ThenInclude(d => d.User)
            .Include(j => j.PickupAddress)
            .Include(j => j.DeliveryAddress)
            .AsSplitQuery()
            .AsQueryable();
        if (query.Status is not null)
            jobs = jobs.Where(j => j.Status == query.Status);
        if (query.JobType is not null)
            jobs = jobs.Where(j => j.JobType == query.JobType);
        if (!string.IsNullOrWhiteSpace(query.Search))
            jobs = jobs.Where(j => j.Order.OrderNumber.Contains(query.Search));

        var (items, total) = await jobs.OrderByDescending(j => j.CreatedAt).PageAsync(query, ct);
        return new PagedResult<DriverJobDto>
        {
            Items = items.Select(j => j.ToDto(j.Order, Market.DriverEarningPerJob)).ToList(),
            Page = query.Page,
            PageSize = query.PageSize,
            TotalCount = total
        };
    }

    /// <summary>Manually assign or reassign a marketplace driver to a job.</summary>
    public async Task<DriverJobDto> AssignDriverAsync(Guid jobId, AssignDriverRequest request, CancellationToken ct)
    {
        var job = await db.DriverJobs
                      .Include(j => j.Order).ThenInclude(o => o.Customer).ThenInclude(c => c.User)
                      .Include(j => j.Order).ThenInclude(o => o.ServiceProvider)
                      .Include(j => j.Order).ThenInclude(o => o.Items)
                      .Include(j => j.PickupAddress)
                      .Include(j => j.DeliveryAddress)
                      .FirstOrDefaultAsync(j => j.Id == jobId, ct)
                  ?? throw new NotFoundException("Job not found.");
        if (job.Status is DriverJobStatus.Completed or DriverJobStatus.Cancelled)
            throw new ConflictException("Completed or cancelled jobs can't be reassigned.");

        var driver = await db.Drivers.Include(d => d.User).FirstOrDefaultAsync(d => d.Id == request.DriverId && d.IsActive, ct)
                     ?? throw new NotFoundException("Driver not found or inactive.");

        var wasPending = job.Status == DriverJobStatus.Pending;
        job.DriverId = driver.Id;
        job.Driver = driver;
        job.AssignedAt = DateTime.UtcNow;
        if (wasPending)
        {
            job.Status = DriverJobStatus.Assigned;
            job.Order.ChangeStatus(job.JobType == DriverJobType.Pickup ? OrderStatus.PickupAssigned : OrderStatus.DeliveryAssigned,
                profile.UserId, $"Admin assigned {driver.User.Name}");
        }
        else
        {
            db.OrderStatusHistory.Add(new OrderStatusHistory
            {
                OrderId = job.OrderId,
                Status = job.Order.Status,
                ChangedByUserId = profile.UserId,
                Notes = $"Admin reassigned {job.JobType.ToString().ToLowerInvariant()} job to {driver.User.Name}"
            });
        }

        db.Notify(driver.UserId, job.Order, "Job assigned", $"You've been assigned the {job.JobType.ToString().ToLowerInvariant()} job for {job.Order.OrderNumber}.");
        await db.SaveChangesAsync(ct);
        return job.ToDto(job.Order, Market.DriverEarningPerJob);
    }

    // ── A11 Payments ─────────────────────────────────────────────────────────

    public async Task<PagedResult<AdminPaymentDto>> GetPaymentsAsync(PagedRequest request, PaymentStatus? status, CancellationToken ct)
    {
        var query = db.Payments.AsQueryable();
        if (status is not null)
            query = query.Where(p => p.Status == status);
        if (!string.IsNullOrWhiteSpace(request.Search))
            query = query.Where(p => p.Order.OrderNumber.Contains(request.Search) || (p.TransactionId ?? "").Contains(request.Search));

        var total = await query.CountAsync(ct);
        var items = await query.OrderByDescending(p => p.CreatedAt)
            .Skip((request.Page - 1) * request.PageSize).Take(request.PageSize)
            .Select(p => new AdminPaymentDto
            {
                Id = p.Id,
                OrderId = p.OrderId,
                OrderNumber = p.Order.OrderNumber,
                CustomerName = p.Order.Customer.User.Name,
                Amount = p.Amount,
                Method = p.Method,
                TransactionId = p.TransactionId,
                Status = p.Status,
                PaidAt = p.PaidAt,
                CreatedAt = p.CreatedAt
            })
            .ToListAsync(ct);
        return new PagedResult<AdminPaymentDto>
        {
            Items = items,
            Page = request.Page,
            PageSize = request.PageSize,
            TotalCount = total
        };
    }

    // ── A12 Disputes ─────────────────────────────────────────────────────────

    public async Task<PagedResult<DisputeDto>> GetDisputesAsync(AdminDisputesQuery query, CancellationToken ct)
    {
        var disputes = db.Disputes.Include(d => d.Order).Include(d => d.RaisedByUser).AsQueryable();
        if (query.Status is not null)
            disputes = disputes.Where(d => d.Status == query.Status);
        if (!string.IsNullOrWhiteSpace(query.Search))
            disputes = disputes.Where(d => d.Order.OrderNumber.Contains(query.Search) || d.Reason.Contains(query.Search));

        var (items, total) = await disputes.OrderByDescending(d => d.CreatedAt).PageAsync(query, ct);
        return new PagedResult<DisputeDto>
        {
            Items = items.Select(d => d.ToDto()).ToList(),
            Page = query.Page,
            PageSize = query.PageSize,
            TotalCount = total
        };
    }

    public async Task<DisputeDto> ReviewDisputeAsync(Guid disputeId, CancellationToken ct)
    {
        var dispute = await LoadDisputeAsync(disputeId, ct);
        if (dispute.Status == DisputeStatus.Open)
        {
            dispute.Status = DisputeStatus.UnderReview;
            await db.SaveChangesAsync(ct);
        }

        return dispute.ToDto();
    }

    public async Task<DisputeDto> ResolveDisputeAsync(Guid disputeId, ResolveDisputeRequest request, CancellationToken ct)
    {
        if (request.Status is not (DisputeStatus.Resolved or DisputeStatus.Rejected))
            throw new BadRequestException("Status must be Resolved or Rejected.");

        var dispute = await LoadDisputeAsync(disputeId, ct);
        dispute.Status = request.Status;
        dispute.Resolution = request.Resolution.Trim();

        var order = dispute.Order;
        if (request.OrderStatus is { } newStatus && newStatus != order.Status)
            order.ForceStatus(newStatus, profile.UserId, $"Dispute {request.Status.ToString().ToLowerInvariant()}: {dispute.Resolution}");

        var customerUserId = await db.Customers.Where(c => c.Id == order.CustomerId).Select(c => c.UserId).FirstAsync(ct);
        var providerUserId = await db.ServiceProviders.Where(p => p.Id == order.ServiceProviderId).Select(p => p.UserId).FirstAsync(ct);
        foreach (var userId in new[] { customerUserId, providerUserId }.Distinct())
            db.Notify(userId, order, "Dispute updated", $"The dispute on {order.OrderNumber} was {request.Status.ToString().ToLowerInvariant()}: {dispute.Resolution}", NotificationType.System);

        await db.SaveChangesAsync(ct);
        return dispute.ToDto();
    }

    private async Task<Dispute> LoadDisputeAsync(Guid disputeId, CancellationToken ct) =>
        await db.Disputes.Include(d => d.Order).Include(d => d.RaisedByUser).FirstOrDefaultAsync(d => d.Id == disputeId, ct)
        ?? throw new NotFoundException("Dispute not found.");
}
