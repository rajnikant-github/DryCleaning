namespace DryCleaning.Api.Domain;

/// <summary>One extra charge as it was on this order when it was placed (name and amount are copied, not linked).</summary>
public class OrderExtraCharge : EntityBase
{
    public Guid OrderId { get; set; }
    public string Name { get; set; } = string.Empty;
    public decimal Amount { get; set; }
    public int DisplayOrder { get; set; }

    public Order Order { get; set; } = null!;
}
