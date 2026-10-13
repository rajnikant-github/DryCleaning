namespace DryCleaning.Models;

public sealed class DriverEarnings
{
    public EarningsPeriod Period { get; init; }
    public string PeriodText { get; init; } = string.Empty;
    public decimal Total { get; init; }
    public int JobCount { get; init; }
    public int PickupCount { get; init; }
    public int DeliveryCount { get; init; }
    public decimal RatePerJob { get; init; }
    public decimal AveragePerJob => JobCount == 0 ? 0 : Total / JobCount;
    public IReadOnlyList<EarningsBar> Bars { get; init; } = [];
    public IReadOnlyList<DriverJobSummary> Jobs { get; init; } = [];
}
