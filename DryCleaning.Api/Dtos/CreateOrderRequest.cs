using DryCleaning.Api.Domain;

namespace DryCleaning.Api.Dtos;

/// <summary>Pickup address is the customer's current address (V1: one address per role).</summary>
public sealed class CreateOrderRequest
{
    public Guid ServiceProviderId { get; set; }
    public IReadOnlyList<CreateOrderItemRequest> Items { get; set; } = [];

    /// <summary>Requested pickup date (today or later).</summary>
    public DateOnly? PickupDate { get; set; }

    /// <summary>Requested pickup window label, e.g. "09:00 AM – 11:00 AM".</summary>
    public string? PickupSlot { get; set; }
}
