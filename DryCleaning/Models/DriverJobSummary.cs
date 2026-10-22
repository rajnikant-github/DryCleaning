namespace DryCleaning.Models;

public sealed class DriverJobSummary
{
    public Guid Id { get; init; }
    public Guid OrderId { get; init; }
    public string OrderNumber { get; init; } = string.Empty;
    public DriverJobType JobType { get; init; }
    public DriverJobStatus Status { get; init; }
    public OrderStatus OrderStatus { get; init; }

    public string FromName { get; init; } = string.Empty;
    public string FromAddress { get; init; } = string.Empty;
    public string FromArea { get; init; } = string.Empty;
    public string ToName { get; init; } = string.Empty;
    public string ToAddress { get; init; } = string.Empty;
    public string ToArea { get; init; } = string.Empty;

    public int ItemCount { get; init; }
    public string ItemsText { get; init; } = string.Empty;
    public decimal Earnings { get; init; }
    public double DistanceKm { get; init; }
    public DateTime? PickupDate { get; init; }
    public string? PickupSlot { get; init; }

    /// <summary>The customer's note for the driver, e.g. "leave the parcel at the gate".</summary>
    public string? DriverNotes { get; init; }
    public DateTime CreatedAt { get; init; }
    public DateTime? AssignedAt { get; init; }
    public DateTime? CompletedAt { get; init; }
    public bool IsOnHold { get; init; }

    public bool IsPickup => JobType == DriverJobType.Pickup;
    public string TypeIcon => IsPickup ? Helpers.FontAwesome.Box : Helpers.FontAwesome.Truck;
    public string TypeText => IsPickup ? "Pickup" : "Delivery";
}
