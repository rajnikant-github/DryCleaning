using DryCleaning.Models;

namespace DryCleaning.Services.Api.Contracts;

/// <summary>Order card.</summary>
public sealed class OrderSummaryDto
{
    public Guid Id { get; set; }
    public string OrderNumber { get; set; } = string.Empty;
    public OrderStatus Status { get; set; }
    public string CustomerName { get; set; } = string.Empty;
    public string ProviderName { get; set; } = string.Empty;
    public int ItemCount { get; set; }
    public string ItemsText { get; set; } = string.Empty;
    public decimal TotalAmount { get; set; }
    public bool IsPaid { get; set; }
    public bool HasOpenDispute { get; set; }
    public DateTime? PickupDate { get; set; }
    public string? PickupSlot { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}
