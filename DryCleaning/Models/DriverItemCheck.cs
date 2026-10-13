namespace DryCleaning.Models;

/// <summary>What the driver checked at the customer's door (D05).</summary>
public sealed class DriverItemCheck
{
    public Guid OrderItemId { get; init; }
    public string ItemLabel { get; init; } = string.Empty;
    public int ExpectedQuantity { get; init; }
    public int CountedQuantity { get; init; }
    public ItemCondition Condition { get; init; }
    public string? Note { get; init; }
}
