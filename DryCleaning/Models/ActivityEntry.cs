namespace DryCleaning.Models;

public sealed class ActivityEntry
{
    public Guid OrderId { get; init; }
    public string OrderNumber { get; init; } = string.Empty;
    public OrderStatus Status { get; init; }
    public string Text { get; init; } = string.Empty;
    public DateTime At { get; init; }
}
