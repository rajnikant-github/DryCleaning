using DryCleaning.Models;

namespace DryCleaning.Services;

public interface ICustomerOrderService
{
    Task<CustomerHomeData> GetHomeAsync();
    Task<IReadOnlyList<OrderSummary>> GetOrdersAsync(CustomerOrderFilter filter);
    Task<IReadOnlyDictionary<CustomerOrderFilter, int>> GetOrderCountsAsync();
    Task<OrderDetail> GetOrderAsync(Guid orderId);

    /// <summary>The price the server will charge for this cart (delivery fee for the real distance). Null when it can't be worked out.</summary>
    Task<OrderQuote?> QuoteAsync(ICartService cart);

    /// <summary>Creates the order from the cart (prices recalculated). Returns the new order id.</summary>
    Task<Guid> PlaceOrderAsync(ICartService cart);
    Task CancelOrderAsync(Guid orderId, string reason);

    /// <summary>Add, change or clear the note for the driver (allowed until the items are collected).</summary>
    Task UpdateDriverNotesAsync(Guid orderId, string? notes);

    /// <summary>Rate the provider after the order is completed: 1 to 5 stars and an optional comment. One rating per order.</summary>
    Task RateOrderAsync(Guid orderId, int stars, string? comment);
    Task RaiseDisputeAsync(Guid orderId, string reason, string description);
}
