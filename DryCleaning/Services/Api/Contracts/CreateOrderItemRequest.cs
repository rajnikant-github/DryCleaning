using DryCleaning.Models;

namespace DryCleaning.Services.Api.Contracts;

/// <summary>Order line in CreateOrderRequest.</summary>
public sealed class CreateOrderItemRequest
{
    public Guid ServiceItemId { get; set; }
    public int Quantity { get; set; }
    public string? Notes { get; set; }
}
