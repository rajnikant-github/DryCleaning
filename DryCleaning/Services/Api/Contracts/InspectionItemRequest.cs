using DryCleaning.Models;

namespace DryCleaning.Services.Api.Contracts;

/// <summary>One line of an inspection.</summary>
public sealed class InspectionItemRequest
{
    public Guid OrderItemId { get; set; }
    public int ReceivedQuantity { get; set; }
    public ItemCondition Condition { get; set; }
    public string? Note { get; set; }
}
