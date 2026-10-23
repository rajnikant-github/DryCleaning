using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.Json.Serialization;

namespace DryCleaning.Configuration;

/// <summary>
/// App configuration. Every setting lives in the repository's single git-ignored <c>secrets.json</c>, in the "App" section of each
/// environment block (local, development, production). <c>./set-app-env.sh &lt;environment&gt;</c> copies that section into
/// <c>appsettings.secrets.json</c>, which is built into the app and merged over the near-empty <c>appsettings.json</c>.
/// Anything missing falls back to the defaults of the settings classes in this folder.
/// </summary>
public sealed class AppSettings
{
    private const string BaseResource = "DryCleaning.appsettings.json";
    private const string SecretsResource = "DryCleaning.appsettings.secrets.json";

    /// <summary>Name of the environment in use (local, development, production), or empty when none was chosen.</summary>
    public string Environment { get; set; } = string.Empty;

    public CurrencySettings Currency { get; set; } = new();
    public PricingSettings Pricing { get; set; } = new();
    public FeatureSettings Features { get; set; } = new();
    public ApiSettings Api { get; set; } = new();
    public GoogleMapsSettings GoogleMaps { get; set; } = new();

    public static AppSettings Load()
    {
        try
        {
            var merged = ReadResource(BaseResource) ?? new JsonObject();
            if (ReadResource(SecretsResource) is { } secrets)
                Merge(merged, secrets);

            return merged.Deserialize(AppSettingsJsonContext.Default.AppSettings) ?? new AppSettings();
        }
        catch (JsonException)
        {
            return new AppSettings();
        }
    }

    private static JsonObject? ReadResource(string name)
    {
        using var stream = typeof(AppSettings).Assembly.GetManifestResourceStream(name);
        if (stream is null)
            return null;

        return JsonNode.Parse(stream, documentOptions: new JsonDocumentOptions
        {
            CommentHandling = JsonCommentHandling.Skip,
            AllowTrailingCommas = true
        }) as JsonObject;
    }

    /// <summary>Copies <paramref name="overrides"/> into <paramref name="target"/>; nested sections are merged key by key.</summary>
    internal static void Merge(JsonObject target, JsonObject overrides)
    {
        foreach (var (key, value) in overrides.ToList())
        {
            if (value is JsonObject section && target[key] is JsonObject existing)
                Merge(existing, section);
            else
                target[key] = value?.DeepClone();
        }
    }
}
