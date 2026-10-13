using System.Text.Json;
using System.Text.Json.Serialization;

namespace DryCleaning.Configuration;

public sealed class FeatureSettings
{
    public bool UseMockData { get; set; } = true;

    /// <summary>Shows "simulate driver" buttons so the provider flow can be demoed end-to-end with mock data.</summary>
    public bool ShowDemoActions { get; set; } = true;
}
