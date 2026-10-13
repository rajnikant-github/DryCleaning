using DryCleaning.Models;

namespace DryCleaning.Services;

public sealed class CartLine
{
    public Guid Id { get; } = Guid.NewGuid();
    public Guid ServiceItemId { get; init; }
    public Guid ProductId { get; init; }
    public Guid CategoryId { get; init; }
    public string CategoryName { get; init; } = string.Empty;
    public string ProductName { get; init; } = string.Empty;
    public string ServiceItemName { get; init; } = string.Empty;
    public decimal UnitPrice { get; init; }
    public int EstimatedDays { get; init; }
    public int Quantity { get; set; }
    public string? Notes { get; set; }
    public string? PhotoPath { get; set; }
    public decimal LineTotal => UnitPrice * Quantity;
}
