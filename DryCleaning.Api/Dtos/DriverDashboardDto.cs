using DryCleaning.Api.Domain;

namespace DryCleaning.Api.Dtos;

// Driver

public sealed class DriverDashboardDto
{
    public required string DriverName { get; init; }
    public required bool IsAvailable { get; init; }
    public required int AvailableJobs { get; init; }
    public required DriverJobDetailDto? ActiveJob { get; init; }
    public required int CompletedToday { get; init; }
    public required decimal EarningsToday { get; init; }
    public required int UnreadNotifications { get; init; }
    public required IReadOnlyList<DriverJobDto> TodaysJobs { get; init; }
    public required string Currency { get; init; }
}
