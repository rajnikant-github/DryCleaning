namespace DryCleaning.Admin.Models;

public sealed class DeliveryPricingDto
{
    public decimal BaseFee { get; set; }
    public decimal PricePerKm { get; set; }
    public decimal IncludedKm { get; set; }
    public decimal MinFee { get; set; }
    public decimal? MaxFee { get; set; }
    public string Currency { get; set; } = "ZAR";
}
