namespace DryCleaning.Models;

public sealed class InspectionResult
{
    public Guid OrderItemId { get; init; }
    public string ItemLabel { get; init; } = string.Empty;
    public int ExpectedQuantity { get; init; }
    public int ReceivedQuantity { get; init; }
    public ItemCondition Condition { get; init; }
    public string? Note { get; init; }
    public IReadOnlyList<string> PhotoPaths { get; init; } = [];
}
