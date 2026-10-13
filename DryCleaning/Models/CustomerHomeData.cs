namespace DryCleaning.Models;

public sealed class CustomerHomeData
{
    public string FirstName { get; init; } = string.Empty;
    public Address? Address { get; init; }
    public IReadOnlyList<Category> Categories { get; init; } = [];
    public OrderSummary? ActiveOrder { get; init; }
    public IReadOnlyList<OrderSummary> RecentOrders { get; init; } = [];
    public IReadOnlyList<PopularServiceItem> PopularItems { get; init; } = [];
    public int UnreadNotifications { get; init; }
}
