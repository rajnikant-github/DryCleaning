using DryCleaning.Api.Domain;

namespace DryCleaning.Api.Dtos;

public sealed class ExtraChargeRequest
{
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }

    /// <summary>Fixed amount, or a percentage of the items (optionally plus delivery).</summary>
    public ExtraChargeType Type { get; set; } = ExtraChargeType.Fixed;

    /// <summary>The fixed amount in the marketplace currency (Type = Fixed).</summary>
    public decimal Amount { get; set; }

    /// <summary>The percentage, e.g. 5 for 5% (Type = Percentage).</summary>
    public decimal Percent { get; set; }

    public ChargeBase PercentBase { get; set; } = ChargeBase.ItemsSubtotal;

    /// <summary>True = VAT is charged on this extra charge too.</summary>
    public bool IsTaxable { get; set; }

    public bool IsActive { get; set; } = true;
    public int DisplayOrder { get; set; }
}
