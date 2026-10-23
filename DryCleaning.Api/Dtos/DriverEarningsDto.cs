using DryCleaning.Api.Domain;

namespace DryCleaning.Api.Dtos;

/// <summary>D13 – earnings are the pay stored on each completed job (set by the admin portal's driver pay setting when the job was created; no payouts table in V1).</summary>
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
