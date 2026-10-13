using DryCleaning.Api.Domain;

namespace DryCleaning.Api.Dtos;

public sealed class AdminDashboardDto
{
    public required int TotalCustomers { get; init; }
    public required int TotalProviders { get; init; }
    public required int TotalDrivers { get; init; }
    public required int ActiveOrders { get; init; }
    public required int CompletedOrders { get; init; }
    public required decimal Revenue { get; init; }
    public required int OpenDisputes { get; init; }
    public required string Currency { get; init; }
    public required IReadOnlyDictionary<string, int> OrdersByStatus { get; init; }
    public required IReadOnlyList<DailyCountDto> OrdersLast30Days { get; init; }
}
