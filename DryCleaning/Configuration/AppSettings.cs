using System.Text.Json;
using System.Text.Json.Serialization;

namespace DryCleaning.Configuration;

/// <summary>
/// App configuration loaded from the embedded <c>appsettings.json</c> at the project root.
/// Change currency, VAT, feature flags or API URL there – no code changes needed.
/// </summary>
public sealed class AppSettings
{
    public CurrencySettings Currency { get; set; } = new();
    public PricingSettings Pricing { get; set; } = new();
    public FeatureSettings Features { get; set; } = new();
    public ApiSettings Api { get; set; } = new();

    public static AppSettings Load()
    {
        try
        {
            using var stream = typeof(AppSettings).Assembly.GetManifestResourceStream("DryCleaning.appsettings.json");
            if (stream is null)
                return new AppSettings();

            return JsonSerializer.Deserialize(stream, AppSettingsJsonContext.Default.AppSettings) ?? new AppSettings();
        }
        catch (JsonException)
        {
            return new AppSettings();
        }
    }
}

public sealed class CurrencySettings
{
    /// <summary>ISO 4217 code, e.g. ZAR.</summary>
    public string Code { get; set; } = "ZAR";

    /// <summary>Symbol shown in the UI, e.g. R.</summary>
    public string Symbol { get; set; } = "R";

    /// <summary>Culture used for number grouping/decimal separators, e.g. en-ZA.</summary>
    public string Culture { get; set; } = "en-ZA";

    public int DecimalPlaces { get; set; } = 2;
}

public sealed class PricingSettings
{
    /// <summary>VAT rate applied to the order subtotal (0.15 = 15%).</summary>
    public decimal VatRate { get; set; } = 0.15m;

    public decimal DefaultDeliveryFee { get; set; } = 45m;

    /// <summary>What a marketplace driver earns per completed pickup or delivery job.</summary>
    public decimal DriverEarningPerJob { get; set; } = 35m;
}

public sealed class FeatureSettings
{
    public bool UseMockData { get; set; } = true;

    /// <summary>Shows "simulate driver" buttons so the provider flow can be demoed end-to-end with mock data.</summary>
    public bool ShowDemoActions { get; set; } = true;
}

public sealed class ApiSettings
{
    public string BaseUrl { get; set; } = "https://edrycleaners.co.za/api/";
}

[JsonSerializable(typeof(AppSettings))]
[JsonSourceGenerationOptions(PropertyNameCaseInsensitive = true, ReadCommentHandling = JsonCommentHandling.Skip)]
internal sealed partial class AppSettingsJsonContext : JsonSerializerContext
{
}
