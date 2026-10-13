namespace DryCleaning.Api.Domain;

public class OrderItem : EntityBase
{
    public Guid OrderId { get; set; }
    public Guid ServiceItemId { get; set; }
    public Guid ProductId { get; set; }
    public int Quantity { get; set; }
    public decimal UnitPrice { get; set; }
    public decimal TotalPrice { get; set; }

    /// <summary>Customer's special instructions (schema addition: OrderItems.Notes).</summary>
    public string? Notes { get; set; }

    public Order Order { get; set; } = null!;
    public ServiceItem ServiceItem { get; set; } = null!;
    public Product Product { get; set; } = null!;
}
