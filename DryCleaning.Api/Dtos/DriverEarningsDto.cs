using DryCleaning.Api.Domain;

namespace DryCleaning.Api.Dtos;

/// <summary>D13 – earnings are completed jobs × Marketplace:DriverEarningPerJob (no payouts table in V1).</summary>
public sealed class DriverEarningsDto
{
    public required EarningsPeriod Period { get; init; }
    public required DateTime From { get; init; }
    public required DateTime To { get; init; }
    public required decimal Total { get; init; }
    public required int JobCount { get; init; }
    public required int PickupCount { get; init; }
    public required int DeliveryCount { get; init; }
    public required decimal RatePerJob { get; init; }
    public required decimal AveragePerJob { get; init; }
    public required string Currency { get; init; }
    public required IReadOnlyList<EarningsBarDto> Bars { get; init; }
    public required IReadOnlyList<DriverJobDto> Jobs { get; init; }
}
