using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.Json.Serialization;

namespace DryCleaning.Configuration;

/// <summary>
/// App configuration loaded from the embedded <c>appsettings.json</c> at the project root.
/// Change currency, VAT, feature flags or API URL there – no code changes needed.
/// <c>./set-app-env.sh &lt;environment&gt;</c> writes <c>appsettings.env.json</c>; its values override
/// <c>appsettings.json</c> (sections are merged, so a profile only lists what differs).
/// Keys such as GoogleMaps:ApiKey live only on the developer's machine in <c>environments/&lt;name&gt;.secrets.json</c>
/// (git-ignored); set-app-env copies the chosen environment's file to <c>appsettings.secrets.json</c>, merged last.
/// </summary>
public sealed class AppSettings
{
    private const string BaseResource = "DryCleaning.appsettings.json";
    private const string EnvironmentResource = "DryCleaning.appsettings.env.json";
    private const string SecretsResource = "DryCleaning.appsettings.secrets.json";

    /// <summary>Name of the environment profile in use (mock, local, staging, production), or empty for plain appsettings.json.</summary>
    public string Environment { get; set; } = string.Empty;

    public CurrencySettings Currency { get; set; } = new();
    public PricingSettings Pricing { get; set; } = new();
    public FeatureSettings Features { get; set; } = new();
    public ApiSettings Api { get; set; } = new();
    public GoogleMapsSettings GoogleMaps { get; set; } = new();

    /// <summary>Demo accounts pre-filled on the Login screen in Debug builds (from the environment profile).</summary>
    public DebugLoginSettings DebugLogins { get; set; } = new();

    public static AppSettings Load()
    {
        try
        {
            var merged = ReadResource(BaseResource) ?? new JsonObject();
            if (ReadResource(EnvironmentResource) is { } overrides)
                Merge(merged, overrides);
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
