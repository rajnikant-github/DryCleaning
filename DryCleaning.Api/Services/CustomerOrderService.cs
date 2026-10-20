using System.Security.Cryptography;
using DryCleaning.Api.Common;
using DryCleaning.Api.Data;
using DryCleaning.Api.Domain;
using DryCleaning.Api.Dtos;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace DryCleaning.Api.Services;

/// <summary>Customer order flow: quote → create → pay → track → confirm delivery with OTP.</summary>
public sealed class CustomerOrderService(AppDbContext db, CurrentProfile profile, IOptions<MarketplaceOptions> marketplace, WorkingHoursService workingHours,
    DeliveryPricingService deliveryPricing)
{
    private MarketplaceOptions Market => marketplace.Value;

    public async Task<OrderQuoteDto> QuoteAsync(CreateOrderRequest request, CancellationToken ct)
    {
        var customer = await profile.CustomerAsync(ct);
        var (order, distanceKm) = await BuildOrderAsync(request, customer.Address, ct);
        return new OrderQuoteDto
        {
            SubTotal = order.SubTotal,
            Tax = order.Tax,
            DeliveryFee = order.DeliveryFee,
            TotalAmount = order.TotalAmount,
            Currency = Market.Currency,
            DistanceKm = distanceKm
        };
    }

    public async Task<OrderDetailDto> CreateAsync(CreateOrderRequest request, CancellationToken ct)
    {
        var customer = await profile.CustomerAsync(ct);
        if (customer.AddressId is null)
            throw new BadRequestException("Add your pickup address before placing an order.");

        var (order, _) = await BuildOrderAsync(request, customer.Address, ct);
        order.CustomerId = customer.Id;
        order.PickupAddressId = customer.AddressId.Value;
        order.OrderNumber = await db.NextOrderNumberAsync(ct);
        order.MarkCreated(customer.UserId);

        // Codes the customer shares with drivers: QR/pickup code and delivery OTP.
        order.Verifications.Add(new OrderVerification { OrderId = order.Id, Type = VerificationType.QR, OtpCode = NewCode() });
        order.Verifications.Add(new OrderVerification { OrderId = order.Id, Type = VerificationType.OTP, OtpCode = NewCode() });

        db.Orders.Add(order);
        await db.SaveChangesAsync(ct);
        return await GetAsync(order.Id, ct);
    }

    public async Task<PagedResult<OrderSummaryDto>> GetMineAsync(CustomerOrdersQuery query, CancellationToken ct)
    {
        var customer = await profile.CustomerAsync(ct);
        var orders = db.Orders.Where(o => o.CustomerId == customer.Id);

        orders = query.Filter switch
        {
            CustomerOrderFilter.Active => orders.Where(o => o.Status != OrderStatus.Completed && o.Status != OrderStatus.Cancelled && o.Status != OrderStatus.Delivered),
            CustomerOrderFilter.Completed => orders.Where(o => o.Status == OrderStatus.Completed || o.Status == OrderStatus.Delivered),
            CustomerOrderFilter.Cancelled => orders.Where(o => o.Status == OrderStatus.Cancelled),
            _ => orders
        };
        if (!string.IsNullOrWhiteSpace(query.Search))
            orders = orders.Where(o => o.OrderNumber.Contains(query.Search));

        var (items, total) = await orders.WithSummaryIncludes().OrderByDescending(o => o.CreatedAt).PageAsync(query, ct);
        return new PagedResult<OrderSummaryDto>
        {
            Items = items.Select(o => o.ToSummary()).ToList(),
            Page = query.Page,
            PageSize = query.PageSize,
            TotalCount = total
        };
    }

    /// <summary>C17 – badge counts for the Active / Completed / Cancelled tabs.</summary>
    public async Task<CustomerOrderCountsDto> GetCountsAsync(CancellationToken ct)
    {
        var customer = await profile.CustomerAsync(ct);
        var byStatus = await db.Orders
            .Where(o => o.CustomerId == customer.Id)
            .GroupBy(o => o.Status)
            .Select(g => new { Status = g.Key, Count = g.Count() })
            .ToListAsync(ct);

        int Count(params OrderStatus[] statuses) => byStatus.Where(s => statuses.Contains(s.Status)).Sum(s => s.Count);
        var completed = Count(OrderStatus.Completed, OrderStatus.Delivered);
        var cancelled = Count(OrderStatus.Cancelled);
        return new CustomerOrderCountsDto
        {
            Active = byStatus.Sum(s => s.Count) - completed - cancelled,
            Completed = completed,
            Cancelled = cancelled
        };
    }

    public async Task<OrderDetailDto> GetAsync(Guid orderId, CancellationToken ct)
    {
        var customer = await profile.CustomerAsync(ct);
        var order = await db.Orders.WithDetailIncludes().FirstOrDefaultAsync(o => o.Id == orderId && o.CustomerId == customer.Id, ct)
                    ?? throw new NotFoundException("Order not found.");
        return order.ToDetail(Market.Currency, Market.DriverEarningPerJob, includeCustomerCodes: true);
    }

    public async Task<OrderDetailDto> CancelAsync(Guid orderId, ReasonRequest request, CancellationToken ct)
    {
        var customer = await profile.CustomerAsync(ct);
        var order = await db.Orders
                        .Include(o => o.ServiceProvider)
                        .Include(o => o.DriverJobs)
                        .FirstOrDefaultAsync(o => o.Id == orderId && o.CustomerId == customer.Id, ct)
                    ?? throw new NotFoundException("Order not found.");

        if (order.Status is not (OrderStatus.Created or OrderStatus.Confirmed or OrderStatus.PickupAssigned))
            throw new ConflictException("This order can no longer be cancelled because the items have been collected.");

        order.ChangeStatus(OrderStatus.Cancelled, customer.UserId, $"Cancelled by customer: {request.Reason}");
        foreach (var job in order.DriverJobs.Where(j => j.Status is DriverJobStatus.Pending or DriverJobStatus.Assigned))
            job.Status = DriverJobStatus.Cancelled;

        db.Notify(order.ServiceProvider.UserId, order, "Order cancelled", $"{order.OrderNumber} was cancelled by the customer.");
        await db.SaveChangesAsync(ct);
        return await GetAsync(orderId, ct);
    }

    public async Task<DisputeDto> RaiseDisputeAsync(Guid orderId, IssueRequest request, CancellationToken ct)
    {
        var customer = await profile.CustomerAsync(ct);
        var order = await db.Orders.Include(o => o.ServiceProvider)
                        .FirstOrDefaultAsync(o => o.Id == orderId && o.CustomerId == customer.Id, ct)
                    ?? throw new NotFoundException("Order not found.");

        var dispute = new Dispute { OrderId = order.Id, RaisedByUserId = customer.UserId, Reason = request.Reason.Trim(), Description = request.Description };
        db.Disputes.Add(dispute);
        order.ChangeStatus(OrderStatus.Disputed, customer.UserId, $"Dispute raised by customer: {request.Reason}");
        db.Notify(order.ServiceProvider.UserId, order, "Dispute raised", $"The customer raised a dispute on {order.OrderNumber}.", NotificationType.System);
        await db.SaveChangesAsync(ct);

        await db.Entry(dispute).Reference(d => d.RaisedByUser).LoadAsync(ct);
        dispute.Order = order;
        return dispute.ToDto();
    }

    /// <summary>Validates the cart against the provider's active catalog and prices it server-side.</summary>
    private async Task<(Order Order, decimal? DistanceKm)> BuildOrderAsync(CreateOrderRequest request, Address? pickupAddress, CancellationToken ct)
    {
        var provider = await db.ServiceProviders.Include(p => p.Address).FirstOrDefaultAsync(p => p.Id == request.ServiceProviderId && p.IsActive, ct)
                       ?? throw new NotFoundException("Service provider not found.");

        if (request.PickupDate is { } pickupDate)
            await workingHours.EnsurePickupAllowedAsync(provider.Id, pickupDate, request.PickupSlot, ct);

        var ids = request.Items.Select(i => i.ServiceItemId).Distinct().ToList();
        var serviceItems = await db.ServiceItems
            .Include(s => s.Product).ThenInclude(p => p.Category)
            .Where(s => ids.Contains(s.Id))
            .ToListAsync(ct);

        var order = new Order
        {
            ServiceProviderId = provider.Id,
            PickupDate = request.PickupDate?.ToDateTime(TimeOnly.MinValue),
            PickupSlot = string.IsNullOrWhiteSpace(request.PickupSlot) ? null : request.PickupSlot.Trim()
        };

        // One order line per cart line, so each keeps its own special instructions.
        foreach (var line in request.Items)
        {
            var item = serviceItems.FirstOrDefault(s => s.Id == line.ServiceItemId)
                       ?? throw new BadRequestException($"Service item {line.ServiceItemId} was not found.");
            if (item.Product.ServiceProviderId != provider.Id)
                throw new BadRequestException($"'{item.Name}' is not offered by {provider.BusinessName}.");
            if (!item.IsActive || !item.Product.IsActive || !item.Product.Category.IsActive)
                throw new BadRequestException($"'{item.Product.Name} – {item.Name}' is currently unavailable.");

            var quantity = line.Quantity;
            order.Items.Add(new OrderItem
            {
                OrderId = order.Id,
                ServiceItemId = item.Id,
                ProductId = item.ProductId,
                Quantity = quantity,
                UnitPrice = item.Price,
                TotalPrice = item.Price * quantity,
                Notes = string.IsNullOrWhiteSpace(line.Notes) ? null : line.Notes.Trim()
            });
        }

        // The fee comes from the admin's pricing rule and the distance between pickup address and provider; it is stored on the order.
        var (fee, distanceKm) = await deliveryPricing.QuoteAsync(pickupAddress, provider.Address, ct);
        order.Recalculate(Market.VatRate, fee);
        return (order, distanceKm);
    }

    private static string NewCode() => RandomNumberGenerator.GetInt32(0, 10000).ToString("0000");
}
