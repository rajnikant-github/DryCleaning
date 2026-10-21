using DryCleaning.Api.Domain;

namespace DryCleaning.Api.Dtos;

public sealed class ExtraChargeDto
{
    public required Guid Id { get; init; }
    public required string Name { get; init; }
    public required string? Description { get; init; }
    public required ExtraChargeType Type { get; init; }

    /// <summary>The fixed amount (Type = Fixed).</summary>
    public required decimal Amount { get; init; }

    /// <summary>The percentage, e.g. 5 for 5% (Type = Percentage).</summary>
    public required decimal Percent { get; init; }

    public required ChargeBase PercentBase { get; init; }
    public required bool IsTaxable { get; init; }
    public required bool IsActive { get; init; }
    public required int DisplayOrder { get; init; }
}
