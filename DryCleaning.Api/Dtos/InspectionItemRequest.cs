using DryCleaning.Api.Domain;

namespace DryCleaning.Api.Dtos;

public sealed class InspectionItemRequest
{
    public Guid OrderItemId { get; set; }
    public int ReceivedQuantity { get; set; }
    public ItemCondition Condition { get; set; }
    public string? Note { get; set; }
}
