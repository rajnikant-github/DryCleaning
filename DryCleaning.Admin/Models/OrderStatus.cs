namespace DryCleaning.Admin.Models;

public enum OrderStatus
{
    Created,
    Confirmed,
    PickupAssigned,
    PickedUp,
    ReceivedByProvider,
    Processing,
    QualityCheck,
    ReadyForDelivery,
    DeliveryAssigned,
    OutForDelivery,
    Delivered,
    Completed,
    Cancelled,
    Disputed
}
