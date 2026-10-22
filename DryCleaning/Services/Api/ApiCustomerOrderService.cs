using System.Diagnostics;
using DryCleaning.Models;
using DryCleaning.Services.Api.Contracts;

namespace DryCleaning.Services.Api;

/// <summary>Customer orders: home, list, detail, place, cancel, dispute.</summary>
public sealed class ApiCustomerOrderService(ApiClient api, ISessionService session) : ICustomerOrderService
{
    private static ApiJsonContext Json => ApiJsonContext.Default;

    public async Task<CustomerHomeData> GetHomeAsync()
    {
        var addressTask = api.GetOrDefaultAsync("api/me/address", Json.AddressDto);
        var categoriesTask = api.GetAsync("api/categories", Json.ListCategoryDto, authorize: false);
        var activeTask = api.GetAsync("api/customer/orders?filter=Active&pageSize=1", Json.PagedResultOrderSummaryDto);
        var recentTask = api.GetAsync("api/customer/orders?filter=All&pageSize=4", Json.PagedResultOrderSummaryDto);
        var popularTask = api.GetAsync("api/catalog/popular?take=4", Json.ListPopularServiceItemDto, authorize: false);
        var unreadTask = api.GetAsync("api/notifications/unread-count", Json.Int32);
        await Task.WhenAll(addressTask, categoriesTask, activeTask, recentTask, popularTask, unreadTask);

        var active = activeTask.Result.Items.FirstOrDefault();
        var name = session.CurrentUser?.Name ?? string.Empty;
        return new CustomerHomeData
        {
            FirstName = name.Split(' ', StringSplitOptions.RemoveEmptyEntries).FirstOrDefault() ?? string.Empty,
            Address = addressTask.Result is { } address ? ApiMapping.ToAddress(address) : null,
            Categories = categoriesTask.Result.Where(c => c.IsActive).OrderBy(c => c.DisplayOrder).Select(ApiMapping.ToCategory).ToList(),
            ActiveOrder = active is null ? null : ApiMapping.ToSummary(active),
            RecentOrders = recentTask.Result.Items.Where(o => o.Id != active?.Id).Take(3).Select(ApiMapping.ToSummary).ToList(),
            PopularItems = popularTask.Result.Select(ApiMapping.ToPopular).ToList(),
            UnreadNotifications = unreadTask.Result
        };
    }

    public async Task<IReadOnlyList<OrderSummary>> GetOrdersAsync(CustomerOrderFilter filter)
    {
        var page = await api.GetAsync($"api/customer/orders?filter={filter}&pageSize=100", Json.PagedResultOrderSummaryDto);
        return page.Items.Select(ApiMapping.ToSummary).ToList();
    }

    public async Task<IReadOnlyDictionary<CustomerOrderFilter, int>> GetOrderCountsAsync()
    {
        var counts = await api.GetAsync("api/customer/orders/counts", Json.CustomerOrderCountsDto);
        return new Dictionary<CustomerOrderFilter, int>
        {
            [CustomerOrderFilter.Active] = counts.Active,
            [CustomerOrderFilter.Completed] = counts.Completed,
            [CustomerOrderFilter.Cancelled] = counts.Cancelled
        };
    }

    public async Task<OrderDetail> GetOrderAsync(Guid orderId) =>
        ApiMapping.ToDetail(await api.GetAsync($"api/customer/orders/{orderId}", Json.OrderDetailDto), api.AbsoluteUrl);

    public async Task<OrderQuote?> QuoteAsync(ICartService cart)
    {
        if (cart.IsEmpty || cart.ProviderId is null)
            return null;

        // The server prices against the customer's saved pickup address, the same address the order will use.
        var request = new CreateOrderRequest
        {
            ServiceProviderId = cart.ProviderId.Value,
            Items = cart.Lines.Select(l => new CreateOrderItemRequest { ServiceItemId = l.ServiceItemId, Quantity = l.Quantity }).ToList()
        };
        var dto = await api.SendAsync(HttpMethod.Post, "api/customer/orders/quote", request, Json.CreateOrderRequest, Json.OrderQuoteDto);
        return new OrderQuote
        {
            SubTotal = dto.SubTotal,
            Tax = dto.Tax,
            VatRate = dto.VatRate,
            DeliveryFee = dto.DeliveryFee,
            ExtraCharges = dto.ExtraCharges.Select(c => new ExtraChargeLine { Name = c.Name, Amount = c.Amount, Basis = c.Basis }).ToList(),
            Total = dto.TotalAmount,
            DistanceKm = dto.DistanceKm,
            DistanceIsEstimate = dto.DistanceIsEstimate,
            DurationMinutes = dto.DurationMinutes
        };
    }

    public async Task<Guid> PlaceOrderAsync(ICartService cart)
    {
        if (cart.IsEmpty || cart.ProviderId is null)
            throw new InvalidOperationException("Your cart is empty.");
        if (cart.PickupAddress is null)
            throw new InvalidOperationException("Choose a pickup address.");
        if (cart.PickupDate is null || string.IsNullOrEmpty(cart.PickupSlot))
            throw new InvalidOperationException("Choose a pickup time.");

        // V1: the order is collected from the customer's saved address (set on the Pickup Address screen).
        var request = new CreateOrderRequest
        {
            ServiceProviderId = cart.ProviderId.Value,
            PickupDate = System.DateOnly.FromDateTime(cart.PickupDate.Value),
            PickupSlot = cart.PickupSlot,
            DriverNotes = string.IsNullOrWhiteSpace(cart.DriverNotes) ? null : cart.DriverNotes.Trim(),
            Items = cart.Lines.Select(l => new CreateOrderItemRequest
            {
                ServiceItemId = l.ServiceItemId,
                Quantity = l.Quantity,
                Notes = string.IsNullOrWhiteSpace(l.Notes) ? null : l.Notes.Trim()
            }).ToList()
        };

        var order = await api.SendAsync(HttpMethod.Post, "api/customer/orders", request, Json.CreateOrderRequest, Json.OrderDetailDto);

        // Item photos the customer attached in the cart (best effort – the order is already placed).
        try
        {
            await api.UploadPhotosAsync(order.Id, PhotoType.Pickup, cart.Lines.Select(l => l.PhotoPath).OfType<string>());
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"[Orders] Photo upload for {order.OrderNumber} failed: {ex.Message}");
        }

        return order.Id;
    }

    public Task CancelOrderAsync(Guid orderId, string reason) =>
        api.SendAsync(HttpMethod.Post, $"api/customer/orders/{orderId}/cancel", new ReasonRequest { Reason = reason }, Json.ReasonRequest);

    public Task UpdateDriverNotesAsync(Guid orderId, string? notes) =>
        api.SendAsync(HttpMethod.Put, $"api/customer/orders/{orderId}/driver-notes", new DriverNotesRequest { Notes = notes }, Json.DriverNotesRequest);

    public Task RateOrderAsync(Guid orderId, int stars, string? comment) =>
        api.SendAsync(HttpMethod.Post, $"api/customer/orders/{orderId}/rating",
            new SubmitRatingRequest { Stars = stars, Comment = string.IsNullOrWhiteSpace(comment) ? null : comment.Trim() }, Json.SubmitRatingRequest);

    public Task RaiseDisputeAsync(Guid orderId, string reason, string description) =>
        api.SendAsync(HttpMethod.Post, $"api/customer/orders/{orderId}/disputes",
            new IssueRequest { Reason = reason, Description = description }, Json.IssueRequest);
}
