using DryCleaning.Models;

namespace DryCleaning.Services.Api.Contracts;

/// <summary>Order line.</summary>
public sealed class OrderItemDto
{
    public Guid Id { get; set; }
    public Guid ProductId { get; set; }
    public Guid ServiceItemId { get; set; }
    public string CategoryName { get; set; } = string.Empty;
    public string ProductName { get; set; } = string.Empty;
    public string ServiceItemName { get; set; } = string.Empty;
    public int Quantity { get; set; }
    public decimal UnitPrice { get; set; }
    public decimal TotalPrice { get; set; }
    public string? Notes { get; set; }
    public int EstimatedDays { get; set; }
}
