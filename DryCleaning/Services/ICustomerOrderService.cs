using DryCleaning.Models;

namespace DryCleaning.Services;

public interface ICustomerOrderService
{
    Task<CustomerHomeData> GetHomeAsync();
    Task<IReadOnlyList<OrderSummary>> GetOrdersAsync(CustomerOrderFilter filter);
    Task<IReadOnlyDictionary<CustomerOrderFilter, int>> GetOrderCountsAsync();
    Task<OrderDetail> GetOrderAsync(Guid orderId);

    /// <summary>Creates the order from the cart (prices recalculated). Returns the new order id.</summary>
    Task<Guid> PlaceOrderAsync(ICartService cart);
    Task CancelOrderAsync(Guid orderId, string reason);
    Task RaiseDisputeAsync(Guid orderId, string reason, string description);
}
