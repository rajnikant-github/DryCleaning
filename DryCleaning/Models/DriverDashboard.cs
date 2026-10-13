namespace DryCleaning.Models;

public sealed class DriverDashboard
{
    public string DriverName { get; init; } = string.Empty;
    public bool IsOnline { get; init; }
    public int AvailableCount { get; init; }
    public DriverJobDetail? ActiveJob { get; init; }
    public int CompletedToday { get; init; }
    public decimal EarningsToday { get; init; }
    public int UnreadNotifications { get; init; }
    public IReadOnlyList<DriverJobSummary> TodaysJobs { get; init; } = [];
}
