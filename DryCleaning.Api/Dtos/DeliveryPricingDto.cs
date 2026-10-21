namespace DryCleaning.Api.Dtos;

/// <summary>The pickup &amp; delivery price rule: base + per-km beyond the included distance, within min/max.</summary>
public sealed class DeliveryPricingDto
{
    public decimal BaseFee { get; set; }
    /// <summary>Price for each km beyond <see cref="IncludedKm"/>. Must be more than zero: delivery is always charged per kilometre.</summary>
    public decimal PricePerKm { get; set; }
    public decimal IncludedKm { get; set; }
    public decimal MinFee { get; set; }

    /// <summary>Null = no upper limit.</summary>
    public decimal? MaxFee { get; set; }

    public string Currency { get; set; } = "ZAR";
}
