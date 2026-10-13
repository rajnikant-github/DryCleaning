namespace DryCleaning.Models;

public sealed class Order : EntityBase
{
    public string OrderNumber { get; set; } = string.Empty;
    public Guid CustomerId { get; set; }
    public Guid ServiceProviderId { get; set; }
    public Guid PickupAddressId { get; set; }
    public OrderStatus Status { get; set; }
    public decimal SubTotal { get; set; }
    public decimal Tax { get; set; }
    public decimal DeliveryFee { get; set; }
    public decimal TotalAmount { get; set; }

    /// <summary>Requested pickup date (added to schema: Orders.PickupDate).</summary>
    public DateTime? PickupDate { get; set; }

    /// <summary>Requested pickup window, e.g. "09:00 – 11:00" (added to schema: Orders.PickupSlot).</summary>
    public string? PickupSlot { get; set; }
}
