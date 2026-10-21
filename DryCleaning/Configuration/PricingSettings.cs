using System.Text.Json;
using System.Text.Json.Serialization;

namespace DryCleaning.Configuration;

public sealed class PricingSettings
{
    /// <summary>VAT rate applied to the order subtotal (0.15 = 15%).</summary>
    public decimal VatRate { get; set; } = 0.15m;

    /// <summary>Pickup &amp; delivery price per km used by the demo (mock) data only. With the API, the admin portal sets the price.</summary>
    public DeliveryPricingSettings Delivery { get; set; } = new();

    /// <summary>What a marketplace driver earns per completed pickup or delivery job.</summary>
    public decimal DriverEarningPerJob { get; set; } = 35m;
}
