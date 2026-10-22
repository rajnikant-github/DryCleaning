using DryCleaning.Models;

namespace DryCleaning.Services.Api.Contracts;

/// <summary>POST api/customer/orders.</summary>
public sealed class CreateOrderRequest
{
    public Guid ServiceProviderId { get; set; }
    public List<CreateOrderItemRequest> Items { get; set; } = [];
    public DateOnly? PickupDate { get; set; }
    public string? PickupSlot { get; set; }
    public string? DriverNotes { get; set; }
}
