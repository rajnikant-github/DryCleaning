namespace DryCleaning.Api.Dtos;

/// <summary>The VAT rule.</summary>
public sealed class VatSettingDto
{
    /// <summary>VAT as a percentage, e.g. 15 for 15%.</summary>
    public decimal Rate { get; set; }
    public bool IsEnabled { get; set; }

    /// <summary>Also charge VAT on the pickup &amp; delivery fee.</summary>
    public bool AppliesToDelivery { get; set; }
}
