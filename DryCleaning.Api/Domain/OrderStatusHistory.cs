namespace DryCleaning.Api.Domain;

public class OrderStatusHistory : EntityBase
{
    public Guid OrderId { get; set; }
    public OrderStatus Status { get; set; }
    public Guid? ChangedByUserId { get; set; }
    public string? Notes { get; set; }

    public Order Order { get; set; } = null!;
}
