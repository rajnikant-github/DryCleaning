namespace DryCleaning.Api.Domain;

/// <summary>
/// An extra charge the admin adds to every order (for example a service fee): a fixed amount or a percentage of the items, switched on or off in the admin portal.
/// Orders copy the charges that were on when they were placed (<see cref="OrderExtraCharge"/>), so editing or deleting one never changes past orders.
/// </summary>
public class ExtraCharge : EntityBase
{
    public string Name { get; set; } = string.Empty;

    /// <summary>Short explanation for the admin (and the customer, if shown).</summary>
    public string? Description { get; set; }

    public ExtraChargeType Type { get; set; } = ExtraChargeType.Fixed;

    /// <summary>The fixed amount (used when <see cref="Type"/> is Fixed).</summary>
    public decimal Amount { get; set; }

    /// <summary>The percentage, e.g. 5 for 5% (used when <see cref="Type"/> is Percentage).</summary>
    public decimal Percent { get; set; }

    /// <summary>What the percentage is taken from.</summary>
    public ChargeBase PercentBase { get; set; } = ChargeBase.ItemsSubtotal;

    /// <summary>True = VAT is charged on this extra charge too.</summary>
    public bool IsTaxable { get; set; }
    public bool IsActive { get; set; } = true;

    /// <summary>Lowest shows first on the summary.</summary>
    public int DisplayOrder { get; set; }
}
