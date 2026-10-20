namespace DryCleaning.Api.Domain;

/// <summary>
/// The marketplace's pickup &amp; delivery price rule (a single row, edited by admins).
/// Fee = BaseFee + PricePerKm × distance beyond IncludedKm, then kept between MinFee and MaxFee.
/// </summary>
public class DeliveryPricing : EntityBase
{
    public decimal BaseFee { get; set; }
    public decimal PricePerKm { get; set; }

    /// <summary>Distance covered by the base fee before the per-km price starts.</summary>
    public decimal IncludedKm { get; set; }

    public decimal MinFee { get; set; }

    /// <summary>Upper limit for the fee; null means no limit.</summary>
    public decimal? MaxFee { get; set; }
}
