namespace DryCleaning.Models;

public sealed class OrderSummary
{
    public Guid Id { get; init; }
    public string OrderNumber { get; init; } = string.Empty;
    public string CustomerName { get; init; } = string.Empty;
    public OrderStatus Status { get; init; }
    public int ItemCount { get; init; }
    public string ItemsText { get; init; } = string.Empty;
    public decimal TotalAmount { get; init; }
    public DateTime CreatedAt { get; init; }
    public DateTime UpdatedAt { get; init; }
    public string? DriverName { get; init; }
    public bool HasOpenDispute { get; init; }
    public string ProviderName { get; init; } = string.Empty;
    public DateTime? PickupDate { get; init; }
    public string? PickupSlot { get; init; }
    public bool IsPaid { get; init; }
}
