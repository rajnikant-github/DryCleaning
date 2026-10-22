using DryCleaning.Api.Common;
using DryCleaning.Api.Data;
using DryCleaning.Api.Domain;
using DryCleaning.Api.Dtos;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace DryCleaning.Api.Services;

/// <summary>Service Provider workflow (P02–P10).</summary>
public sealed class ProviderOrderService(AppDbContext db, CurrentProfile profile, IOptions<MarketplaceOptions> marketplace)
{
    private MarketplaceOptions Market => marketplace.Value;

    public async Task<ProviderDashboardDto> GetDashboardAsync(CancellationToken ct)
    {
        var provider = await profile.ProviderAsync(ct);
        var orders = Visible(provider.Id);

        var counts = await orders.GroupBy(o => o.Status).Select(g => new { g.Key, Count = g.Count() }).ToListAsync(ct);
        int Count(params OrderStatus[] statuses) => counts.Where(c => statuses.Contains(c.Key)).Sum(c => c.Count);

        var today = DateTime.UtcNow.Date;
        var todays = await orders.WithSummaryIncludes()
            .Where(o => o.UpdatedAt >= today && o.Status != OrderStatus.Cancelled && o.Status != OrderStatus.Completed)
            .OrderByDescending(o => o.UpdatedAt)
            .Take(10)
            .ToListAsync(ct);

        var activity = await db.OrderStatusHistory
            .Where(h => h.Order.ServiceProviderId == provider.Id)
            .OrderByDescending(h => h.CreatedAt)
            .Take(10)
            .Select(h => new ActivityDto
            {
                OrderId = h.OrderId,
                OrderNumber = h.Order.OrderNumber,
                Status = h.Status,
                Notes = h.Notes,
                At = h.CreatedAt
            })
            .ToListAsync(ct);

        var unread = await db.Notifications.CountAsync(n => n.UserId == provider.UserId && !n.IsRead, ct);

        return new ProviderDashboardDto
        {
            BusinessName = provider.BusinessName,
            NewCount = Count(OrderStatus.Created),
            IncomingCount = Count(OrderStatus.Confirmed, OrderStatus.PickupAssigned, OrderStatus.PickedUp),
            InProcessCount = Count(OrderStatus.ReceivedByProvider, OrderStatus.Processing, OrderStatus.QualityCheck),
            ReadyCount = Count(OrderStatus.ReadyForDelivery, OrderStatus.DeliveryAssigned, OrderStatus.OutForDelivery),
            CompletedCount = Count(OrderStatus.Delivered, OrderStatus.Completed),
            UnreadNotifications = unread,
            TodaysOrders = todays.Select(o => o.ToSummary()).ToList(),
            RecentActivity = activity
        };
    }

    public async Task<PagedResult<OrderSummaryDto>> GetOrdersAsync(ProviderOrdersQuery query, CancellationToken ct)
    {
        var provider = await profile.ProviderAsync(ct);
        var orders = Visible(provider.Id);

        orders = query.Filter switch
        {
            ProviderOrderFilter.New => orders.Where(o => o.Status == OrderStatus.Created),
            ProviderOrderFilter.Incoming => orders.Where(o => o.Status == OrderStatus.Confirmed || o.Status == OrderStatus.PickupAssigned || o.Status == OrderStatus.PickedUp),
            ProviderOrderFilter.InProcess => orders.Where(o => o.Status == OrderStatus.ReceivedByProvider || o.Status == OrderStatus.Processing || o.Status == OrderStatus.QualityCheck),
            ProviderOrderFilter.Ready => orders.Where(o => o.Status == OrderStatus.ReadyForDelivery || o.Status == OrderStatus.DeliveryAssigned || o.Status == OrderStatus.OutForDelivery),
            ProviderOrderFilter.Completed => orders.Where(o => o.Status == OrderStatus.Delivered || o.Status == OrderStatus.Completed),
            ProviderOrderFilter.Cancelled => orders.Where(o => o.Status == OrderStatus.Cancelled),
            ProviderOrderFilter.Disputed => orders.Where(o => o.Status == OrderStatus.Disputed),
            _ => orders
        };
        if (!string.IsNullOrWhiteSpace(query.Search))
            orders = orders.Where(o => o.OrderNumber.Contains(query.Search) || o.Customer.User.Name.Contains(query.Search));

        var (items, total) = await orders.WithSummaryIncludes().OrderByDescending(o => o.UpdatedAt).PageAsync(query, ct);
        return new PagedResult<OrderSummaryDto>
        {
            Items = items.Select(o => o.ToSummary()).ToList(),
            Page = query.Page,
            PageSize = query.PageSize,
            TotalCount = total
        };
    }

    /// <summary>P03 – badge counts for every order tab (same rules as <see cref="GetOrdersAsync"/>).</summary>
    public async Task<ProviderOrderCountsDto> GetOrderCountsAsync(CancellationToken ct)
    {
        var provider = await profile.ProviderAsync(ct);
        var byStatus = await Visible(provider.Id)
            .GroupBy(o => o.Status)
            .Select(g => new { Status = g.Key, Count = g.Count() })
            .ToListAsync(ct);

        int Count(params OrderStatus[] statuses) => byStatus.Where(s => statuses.Contains(s.Status)).Sum(s => s.Count);
        return new ProviderOrderCountsDto
        {
            New = Count(OrderStatus.Created),
            Incoming = Count(OrderStatus.Confirmed, OrderStatus.PickupAssigned, OrderStatus.PickedUp),
            InProcess = Count(OrderStatus.ReceivedByProvider, OrderStatus.Processing, OrderStatus.QualityCheck),
            Ready = Count(OrderStatus.ReadyForDelivery, OrderStatus.DeliveryAssigned, OrderStatus.OutForDelivery),
            Completed = Count(OrderStatus.Delivered, OrderStatus.Completed),
            Cancelled = Count(OrderStatus.Cancelled),
            Disputed = Count(OrderStatus.Disputed)
        };
    }

    public async Task<OrderDetailDto> GetOrderAsync(Guid orderId, CancellationToken ct)
    {
        var order = await LoadAsync(orderId, ct, detail: true);
        return order.ToDetail(Market.Currency, Market.DriverEarningPerJob, includeCustomerCodes: false);
    }

    // ── P03/P04 Accept / Reject ──────────────────────────────────────────────

    public async Task<OrderDetailDto> AcceptAsync(Guid orderId, CancellationToken ct)
    {
        var order = await LoadAsync(orderId, ct);
        if (Market.RequirePaymentBeforeAccept && !order.IsPaid())
            throw new ConflictException("This order hasn't been paid yet.");

        order.ChangeStatus(OrderStatus.Confirmed, profile.UserId, "Accepted by provider");

        // Publish a pickup job to the shared driver marketplace (Customer → Provider).
        var provider = order.ServiceProvider;
        if (provider.AddressId is null)
            throw new BadRequestException("Add your business address before accepting orders.");

        db.DriverJobs.Add(new DriverJob
        {
            OrderId = order.Id,
            JobType = DriverJobType.Pickup,
            Status = DriverJobStatus.Pending,
            PickupAddressId = order.PickupAddressId,
            DeliveryAddressId = provider.AddressId.Value
        });

        db.Notify(order.Customer.UserId, order, "Order confirmed", $"{provider.BusinessName} accepted {order.OrderNumber}. We're finding a driver for pickup.");
        await NotifyAvailableDriversAsync(order, "New pickup job", ct);
        await db.SaveChangesAsync(ct);
        return await GetOrderAsync(orderId, ct);
    }

    public async Task<OrderDetailDto> RejectAsync(Guid orderId, ReasonRequest request, CancellationToken ct)
    {
        var order = await LoadAsync(orderId, ct);
        order.ChangeStatus(OrderStatus.Cancelled, profile.UserId, $"Rejected by provider: {request.Reason}");
        db.Notify(order.Customer.UserId, order, "Order rejected",
            $"{order.ServiceProvider.BusinessName} couldn't take {order.OrderNumber}: {request.Reason}. Any payment will be refunded.");
        await db.SaveChangesAsync(ct);
        return await GetOrderAsync(orderId, ct);
    }

    // ── P05 Receive ──────────────────────────────────────────────────────────

    public async Task<OrderDetailDto> ConfirmReceivedAsync(Guid orderId, ReceiveOrderRequest request, CancellationToken ct)
    {
        var order = await LoadAsync(orderId, ct);
        var expected = order.Items.Sum(i => i.Quantity);

        // The provider can only confirm what has arrived after the driver has handed the items over.
        if (!order.DriverHandedOver())
            throw new ConflictException("The driver has not handed this order over yet. You can receive it once the driver taps hand over.");

        db.OrderVerifications.Add(new OrderVerification
        {
            OrderId = order.Id,
            Type = VerificationType.Item,
            VerifiedAt = DateTime.UtcNow,
            VerifiedByUserId = profile.UserId
        });

        var notes = $"Received {request.ActualItemCount} of {expected} items.";
        if (!string.IsNullOrWhiteSpace(request.Notes))
            notes += " " + request.Notes.Trim();
        order.ChangeStatus(OrderStatus.ReceivedByProvider, profile.UserId, notes);

        db.Notify(order.Customer.UserId, order, "Items received", $"{order.ServiceProvider.BusinessName} has received your items for {order.OrderNumber}.");
        await db.SaveChangesAsync(ct);
        return await GetOrderAsync(orderId, ct);
    }

    // ── P06 Inspection ───────────────────────────────────────────────────────

    public async Task<OrderDetailDto> CompleteInspectionAsync(Guid orderId, InspectionRequest request, CancellationToken ct)
    {
        var order = await LoadAsync(orderId, ct);
        var issues = new List<string>();

        foreach (var result in request.Items)
        {
            var item = order.Items.FirstOrDefault(i => i.Id == result.OrderItemId)
                       ?? throw new BadRequestException($"Order item {result.OrderItemId} is not part of this order.");

            var mismatch = result.ReceivedQuantity != item.Quantity;
            if (result.Condition != ItemCondition.Good || mismatch)
            {
                var parts = new List<string> { $"{item.Product.Name} ({item.ServiceItem.Name}): {result.Condition}" };
                if (mismatch) parts.Add($"{result.ReceivedQuantity}/{item.Quantity} pcs");
                if (!string.IsNullOrWhiteSpace(result.Note)) parts.Add(result.Note.Trim());
                issues.Add(string.Join(", ", parts));
            }
        }

        var notes = issues.Count == 0
            ? "Inspection complete · all items in good condition"
            : "Inspection complete · " + string.Join("; ", issues);
        order.ChangeStatus(OrderStatus.Processing, profile.UserId, Truncate(notes, 500));

        db.Notify(order.Customer.UserId, order, "Cleaning started", $"{order.OrderNumber} has been inspected and is now being cleaned.");
        await db.SaveChangesAsync(ct);
        return await GetOrderAsync(orderId, ct);
    }

    // ── P07 / P08 Processing & quality check ─────────────────────────────────

    public async Task<OrderDetailDto> MarkQualityCheckAsync(Guid orderId, CancellationToken ct)
    {
        var order = await LoadAsync(orderId, ct);
        order.ChangeStatus(OrderStatus.QualityCheck, profile.UserId, "Cleaning finished · sent for quality check");
        await db.SaveChangesAsync(ct);
        return await GetOrderAsync(orderId, ct);
    }

    public async Task<OrderDetailDto> SendBackToProcessingAsync(Guid orderId, ReasonRequest request, CancellationToken ct)
    {
        var order = await LoadAsync(orderId, ct);
        order.ChangeStatus(OrderStatus.Processing, profile.UserId, $"Quality check failed: {request.Reason}");
        await db.SaveChangesAsync(ct);
        return await GetOrderAsync(orderId, ct);
    }

    public async Task<OrderDetailDto> MarkReadyForDeliveryAsync(Guid orderId, CancellationToken ct)
    {
        var order = await LoadAsync(orderId, ct);
        order.ChangeStatus(OrderStatus.ReadyForDelivery, profile.UserId, "Quality check passed · packed for delivery");
        db.Notify(order.Customer.UserId, order, "Ready for delivery", $"{order.OrderNumber} is cleaned and packed.");
        await db.SaveChangesAsync(ct);
        return await GetOrderAsync(orderId, ct);
    }

    // ── P09 Request driver ───────────────────────────────────────────────────

    public async Task<OrderDetailDto> RequestDeliveryDriverAsync(Guid orderId, CancellationToken ct)
    {
        var order = await LoadAsync(orderId, ct);
        if (order.Status != OrderStatus.ReadyForDelivery)
            throw new ConflictException("Only orders that are ready for delivery can request a driver.");
        if (order.DriverJobs.Any(j => j.JobType == DriverJobType.Delivery && j.Status is DriverJobStatus.Pending or DriverJobStatus.Assigned or DriverJobStatus.InProgress))
            throw new ConflictException("A delivery driver has already been requested for this order.");
        if (order.ServiceProvider.AddressId is null)
            throw new BadRequestException("Add your business address first.");

        // Provider → Customer job published to the marketplace.
        db.DriverJobs.Add(new DriverJob
        {
            OrderId = order.Id,
            JobType = DriverJobType.Delivery,
            Status = DriverJobStatus.Pending,
            PickupAddressId = order.ServiceProvider.AddressId.Value,
            DeliveryAddressId = order.PickupAddressId
        });

        await NotifyAvailableDriversAsync(order, "New delivery job", ct);
        await db.SaveChangesAsync(ct);
        return await GetOrderAsync(orderId, ct);
    }

    // ── Issues & disputes ────────────────────────────────────────────────────

    /// <summary>Logs a discrepancy/damage report without stopping the order.</summary>
    public async Task<DisputeDto> ReportIssueAsync(Guid orderId, IssueRequest request, CancellationToken ct)
    {
        var order = await LoadAsync(orderId, ct);
        var dispute = new Dispute { OrderId = order.Id, RaisedByUserId = profile.UserId, Reason = request.Reason.Trim(), Description = request.Description };
        db.Disputes.Add(dispute);
        db.OrderStatusHistory.Add(new OrderStatusHistory
        {
            OrderId = order.Id,
            Status = order.Status,
            ChangedByUserId = profile.UserId,
            Notes = Truncate($"Issue reported – {request.Reason}: {request.Description}", 500)
        });
        db.Notify(order.Customer.UserId, order, "Issue reported", $"{order.ServiceProvider.BusinessName} reported an issue on {order.OrderNumber}: {request.Reason}.", NotificationType.System);
        await db.SaveChangesAsync(ct);
        return await LoadDisputeAsync(dispute.Id, ct);
    }

    /// <summary>Raises a dispute and puts the order on hold.</summary>
    public async Task<DisputeDto> RaiseDisputeAsync(Guid orderId, IssueRequest request, CancellationToken ct)
    {
        var order = await LoadAsync(orderId, ct);
        var dispute = new Dispute { OrderId = order.Id, RaisedByUserId = profile.UserId, Reason = request.Reason.Trim(), Description = request.Description };
        db.Disputes.Add(dispute);
        order.ChangeStatus(OrderStatus.Disputed, profile.UserId, Truncate($"{request.Reason}: {request.Description}", 500));
        db.Notify(order.Customer.UserId, order, "Order on hold", $"{order.OrderNumber} is on hold while support reviews an issue.", NotificationType.System);
        await db.SaveChangesAsync(ct);
        return await LoadDisputeAsync(dispute.Id, ct);
    }

    // ── Helpers ──────────────────────────────────────────────────────────────

    /// <summary>Orders the provider may see (unpaid new orders are hidden when payment is required).</summary>
    private IQueryable<Order> Visible(Guid providerId)
    {
        var orders = db.Orders.Where(o => o.ServiceProviderId == providerId);
        if (Market.RequirePaymentBeforeAccept)
            orders = orders.Where(o => o.Status != OrderStatus.Created || o.Payments.Any(p => p.Status == PaymentStatus.Success));
        return orders;
    }

    private async Task<Order> LoadAsync(Guid orderId, CancellationToken ct, bool detail = false)
    {
        var provider = await profile.ProviderAsync(ct);
        IQueryable<Order> query = detail
            ? db.Orders.WithDetailIncludes()
            : db.Orders
                .Include(o => o.Customer)
                .Include(o => o.ServiceProvider)
                .Include(o => o.Items).ThenInclude(i => i.Product)
                .Include(o => o.Items).ThenInclude(i => i.ServiceItem)
                .Include(o => o.Payments)
                .Include(o => o.DriverJobs)
                .AsSplitQuery();

        return await query.FirstOrDefaultAsync(o => o.Id == orderId && o.ServiceProviderId == provider.Id, ct)
               ?? throw new NotFoundException("Order not found.");
    }

    private async Task<DisputeDto> LoadDisputeAsync(Guid disputeId, CancellationToken ct) =>
        (await db.Disputes.Include(d => d.Order).Include(d => d.RaisedByUser).FirstAsync(d => d.Id == disputeId, ct)).ToDto();

    private async Task NotifyAvailableDriversAsync(Order order, string title, CancellationToken ct)
    {
        var driverUserIds = await db.Drivers
            .Where(d => d.IsAvailable && d.IsActive && d.User.IsActive)
            .Select(d => d.UserId)
            .ToListAsync(ct);
        foreach (var userId in driverUserIds)
            db.Notify(userId, order, title, $"{order.OrderNumber} is available near {order.ServiceProvider.BusinessName}.");
    }

    private static string Truncate(string value, int max) => value.Length <= max ? value : value[..(max - 1)] + "…";
}
