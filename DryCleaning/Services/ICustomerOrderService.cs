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
    Task RaiseDisputeAsync(Guid orderId, string reason, string description);
}
