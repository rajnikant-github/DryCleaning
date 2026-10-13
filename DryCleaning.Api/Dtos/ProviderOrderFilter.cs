using DryCleaning.Api.Domain;

namespace DryCleaning.Api.Dtos;

public enum ProviderOrderFilter
{
    /// <summary>Created (and paid) – waiting for Accept / Reject.</summary>
    New,
    /// <summary>Confirmed, PickupAssigned, PickedUp.</summary>
    Incoming,
    /// <summary>ReceivedByProvider, Processing, QualityCheck.</summary>
    InProcess,
    /// <summary>ReadyForDelivery, DeliveryAssigned, OutForDelivery.</summary>
    Ready,
    /// <summary>Delivered, Completed.</summary>
    Completed,
    Cancelled,
    Disputed,
    All
}
