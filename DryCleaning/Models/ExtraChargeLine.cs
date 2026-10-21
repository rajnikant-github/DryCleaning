namespace DryCleaning.Models;

/// <summary>An extra charge on an order or quote: a name, the amount and, for percentage charges, how it was worked out.</summary>
public sealed class ExtraChargeLine
{
    public string Name { get; init; } = string.Empty;
    public decimal Amount { get; init; }

    /// <summary>E.g. "5% of items"; null for fixed amounts.</summary>
    public string? Basis { get; init; }
}
