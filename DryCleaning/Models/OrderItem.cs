namespace DryCleaning.Models;

public sealed class OrderItem : EntityBase
{
    public Guid OrderId { get; set; }
    public Guid ServiceItemId { get; set; }
    public Guid ProductId { get; set; }
    public int Quantity { get; set; }
    public decimal UnitPrice { get; set; }
    public decimal TotalPrice { get; set; }

    /// <summary>Customer's special instructions (added to schema: OrderItems.Notes).</summary>
    public string? Notes { get; set; }
}
