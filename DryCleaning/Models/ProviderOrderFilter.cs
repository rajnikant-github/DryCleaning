namespace DryCleaning.Models;

// Read models returned by the provider services (shape of the future API responses).

public enum ProviderOrderFilter
{
    New,          // Created – waiting for Accept / Reject
    Incoming,     // Confirmed, PickupAssigned, PickedUp – driver bringing items
    InProcess,    // ReceivedByProvider, Processing, QualityCheck
    Ready,        // ReadyForDelivery, DeliveryAssigned, OutForDelivery
    Completed,    // Delivered, Completed
    Cancelled,
    Disputed
}
