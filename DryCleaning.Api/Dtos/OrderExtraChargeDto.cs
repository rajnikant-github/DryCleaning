namespace DryCleaning.Api.Dtos;

/// <summary>An extra charge on an order or quote.</summary>
public sealed class OrderExtraChargeDto
{
    public required string Name { get; init; }
    public required decimal Amount { get; init; }
}
