namespace DryCleaning.Models;

/// <summary>An extra charge on an order or quote: a name and a fixed amount.</summary>
public sealed class ExtraChargeLine
{
    public string Name { get; init; } = string.Empty;
    public decimal Amount { get; init; }
}
