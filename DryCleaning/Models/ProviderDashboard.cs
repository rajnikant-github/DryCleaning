namespace DryCleaning.Models;

public sealed class ProviderDashboard
{
    public string BusinessName { get; init; } = string.Empty;
    public string OwnerName { get; init; } = string.Empty;
    public int NewCount { get; init; }
    public int InProcessCount { get; init; }
    public int ReadyCount { get; init; }
    public int CompletedCount { get; init; }
    public int UnreadNotifications { get; init; }
    public OrderSummary? IncomingDriverOrder { get; init; }
    public IReadOnlyList<OrderSummary> TodaysOrders { get; init; } = [];
    public IReadOnlyList<ActivityEntry> RecentActivity { get; init; } = [];
}
