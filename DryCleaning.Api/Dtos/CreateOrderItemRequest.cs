using DryCleaning.Api.Domain;

namespace DryCleaning.Api.Dtos;

// Orders

public sealed class CreateOrderItemRequest
{
    public Guid ServiceItemId { get; set; }
    public int Quantity { get; set; }
    public string? Notes { get; set; }
}
