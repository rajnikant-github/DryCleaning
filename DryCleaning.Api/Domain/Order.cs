namespace DryCleaning.Api.Domain;

public class Order : EntityBase
{
    public string OrderNumber { get; set; } = string.Empty;
    public Guid CustomerId { get; set; }
    public Guid ServiceProviderId { get; set; }
    public Guid PickupAddressId { get; set; }
    public OrderStatus Status { get; private set; } = OrderStatus.Created;
    public decimal SubTotal { get; set; }
    public decimal Tax { get; set; }
    public decimal DeliveryFee { get; set; }
    public decimal TotalAmount { get; set; }

    /// <summary>Requested pickup date (schema addition: Orders.PickupDate).</summary>
    public DateTime? PickupDate { get; set; }

    /// <summary>Requested pickup window, e.g. "09:00 AM – 11:00 AM" (schema addition: Orders.PickupSlot).</summary>
    public string? PickupSlot { get; set; }

    public Customer Customer { get; set; } = null!;
    public Provider ServiceProvider { get; set; } = null!;
    public Address PickupAddress { get; set; } = null!;
    public ICollection<OrderItem> Items { get; set; } = [];
    public ICollection<OrderExtraCharge> ExtraCharges { get; set; } = [];
    public ICollection<OrderStatusHistory> StatusHistory { get; set; } = [];
    public ICollection<OrderVerification> Verifications { get; set; } = [];
    public ICollection<OrderPhoto> Photos { get; set; } = [];
    public ICollection<Payment> Payments { get; set; } = [];
    public ICollection<DriverJob> DriverJobs { get; set; } = [];
    public ICollection<Dispute> Disputes { get; set; } = [];

    /// <summary>Moves the order to a new status (validated) and records history.</summary>
    public OrderStatusHistory ChangeStatus(OrderStatus to, Guid? changedByUserId, string? notes = null)
    {
        OrderWorkflow.EnsureCanTransition(Status, to);
        Status = to;
        Touch();
        var entry = new OrderStatusHistory
        {
            OrderId = Id,
            Status = to,
            ChangedByUserId = changedByUserId,
            Notes = notes
        };
        StatusHistory.Add(entry);
        return entry;
    }

    /// <summary>Admin-only override (e.g. resolving a dispute). Skips workflow validation but still records history.</summary>
    public OrderStatusHistory ForceStatus(OrderStatus to, Guid? changedByUserId, string notes)
    {
        Status = to;
        Touch();
        var entry = new OrderStatusHistory { OrderId = Id, Status = to, ChangedByUserId = changedByUserId, Notes = notes };
        StatusHistory.Add(entry);
        return entry;
    }

    /// <summary>Records the initial "Created" history row for a new order.</summary>
    public void MarkCreated(Guid? userId)
    {
        Status = OrderStatus.Created;
        StatusHistory.Add(new OrderStatusHistory { OrderId = Id, Status = OrderStatus.Created, ChangedByUserId = userId });
    }

    public void Recalculate(decimal vatRate, decimal deliveryFee)
    {
        SubTotal = Items.Sum(i => i.TotalPrice);
        DeliveryFee = deliveryFee;
        Tax = Math.Round(SubTotal * vatRate, 2, MidpointRounding.AwayFromZero);
        // Extra charges are fixed amounts the admin adds; like the delivery fee they carry no VAT.
        TotalAmount = SubTotal + Tax + DeliveryFee + ExtraCharges.Sum(c => c.Amount);
    }
}
