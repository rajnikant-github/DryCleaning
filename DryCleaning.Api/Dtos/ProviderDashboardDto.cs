using DryCleaning.Api.Domain;

namespace DryCleaning.Api.Dtos;

public sealed class ProviderDashboardDto
{
    public required string BusinessName { get; init; }
    public required int NewCount { get; init; }
    public required int IncomingCount { get; init; }
    public required int InProcessCount { get; init; }
    public required int ReadyCount { get; init; }
    public required int CompletedCount { get; init; }
    public required int UnreadNotifications { get; init; }
    public required IReadOnlyList<OrderSummaryDto> TodaysOrders { get; init; }
    public required IReadOnlyList<ActivityDto> RecentActivity { get; init; }
}
