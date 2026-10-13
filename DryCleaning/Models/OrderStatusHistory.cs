namespace DryCleaning.Models;

public sealed class OrderStatusHistory : EntityBase
{
    public Guid OrderId { get; set; }
    public OrderStatus Status { get; set; }
    public Guid? ChangedByUserId { get; set; }
    public string? Notes { get; set; }
}
