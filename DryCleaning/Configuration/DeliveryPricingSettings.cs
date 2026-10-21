namespace DryCleaning.Configuration;

/// <summary>Per-km pickup &amp; delivery price for demo data: base fee covers <see cref="IncludedKm"/>, then <see cref="PricePerKm"/> for each km.</summary>
public sealed class DeliveryPricingSettings
{
    public decimal BaseFee { get; set; } = 30m;
    public decimal IncludedKm { get; set; } = 2m;
    public decimal PricePerKm { get; set; } = 5m;
    public decimal MinFee { get; set; } = 30m;
    public decimal MaxFee { get; set; } = 250m;

    /// <summary>The fee for a distance, using the same formula as the API.</summary>
    public decimal FeeFor(double km)
    {
        var extra = Math.Max(0m, (decimal)km - IncludedKm);
        var fee = Math.Min(MaxFee, Math.Max(MinFee, BaseFee + extra * PricePerKm));
        return Math.Round(fee, 2, MidpointRounding.AwayFromZero);
    }
}
