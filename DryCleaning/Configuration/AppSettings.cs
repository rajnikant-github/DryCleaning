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
    public GoogleMapsSettings GoogleMaps { get; set; } = new();

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
