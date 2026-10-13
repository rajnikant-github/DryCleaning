using System.Text.Json;
using System.Text.Json.Serialization;

namespace DryCleaning.Configuration;

public sealed class PricingSettings
{
    /// <summary>VAT rate applied to the order subtotal (0.15 = 15%).</summary>
    public decimal VatRate { get; set; } = 0.15m;

    public decimal DefaultDeliveryFee { get; set; } = 45m;

    /// <summary>What a marketplace driver earns per completed pickup or delivery job.</summary>
    public decimal DriverEarningPerJob { get; set; } = 35m;
}
