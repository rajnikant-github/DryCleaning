namespace DryCleaning.Api.Domain;

/// <summary>One extra charge as it was on this order when it was placed (name and amount are copied, not linked).</summary>
public class OrderExtraCharge : EntityBase
{
    public Guid OrderId { get; set; }
    public string Name { get; set; } = string.Empty;
    public decimal Amount { get; set; }

    /// <summary>Set when the charge was a percentage.</summary>
    public decimal? Percent { get; set; }

    /// <summary>How the amount was worked out, e.g. "5% of items".</summary>
    public string? Basis { get; set; }

    public bool IsTaxable { get; set; }
    public int DisplayOrder { get; set; }

    public Order Order { get; set; } = null!;
}
