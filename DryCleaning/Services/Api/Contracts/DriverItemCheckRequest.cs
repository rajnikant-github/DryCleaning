using DryCleaning.Models;

namespace DryCleaning.Services.Api.Contracts;

/// <summary>What the driver counted for one order line.</summary>
public sealed class DriverItemCheckRequest
{
    public Guid OrderItemId { get; set; }
    public int CountedQuantity { get; set; }
    public ItemCondition Condition { get; set; }
    public string? Note { get; set; }
}
