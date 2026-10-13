using DryCleaning.Api.Domain;

namespace DryCleaning.Api.Dtos;

/// <summary>D05 – what the driver counted at the customer's door for one order line.</summary>
public sealed class DriverItemCheckRequest
{
    public Guid OrderItemId { get; set; }
    public int CountedQuantity { get; set; }
    public ItemCondition Condition { get; set; }
    public string? Note { get; set; }
}
