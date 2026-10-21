namespace DryCleaning.Api.Domain;

/// <summary>
/// The marketplace's VAT rule (a single row, edited by admins). VAT is always worked out on the items; it can also be charged on the
/// pickup &amp; delivery fee, and on each extra charge that is marked taxable.
/// </summary>
public class VatSetting : EntityBase
{
    /// <summary>VAT as a percentage, e.g. 15 for 15%.</summary>
    public decimal Rate { get; set; } = 15m;

    /// <summary>False = no VAT is charged on new orders.</summary>
    public bool IsEnabled { get; set; } = true;

    /// <summary>Also charge VAT on the pickup &amp; delivery fee.</summary>
    public bool AppliesToDelivery { get; set; }
}
