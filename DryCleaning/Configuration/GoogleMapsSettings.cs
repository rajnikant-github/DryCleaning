namespace DryCleaning.Configuration;

/// <summary>
/// Google Places (address search) and Geocoding (current location). Leave ApiKey empty, or set
/// UseSampleAddresses, to use the sample Cape Town addresses. The key needs "Places API (New)" and
/// "Geocoding API" enabled.
/// </summary>
public sealed class GoogleMapsSettings
{
    public string ApiKey { get; set; } = string.Empty;

    /// <summary>Limits address suggestions to this country (ISO 3166-1 alpha-2).</summary>
    public string RegionCode { get; set; } = "za";

    public string LanguageCode { get; set; } = "en";

    /// <summary>Use the sample addresses even though a key is set (e.g. while the key's Google project isn't ready).</summary>
    public bool UseSampleAddresses { get; set; }

    public bool IsConfigured => !string.IsNullOrWhiteSpace(ApiKey);

    /// <summary>True when address search should call Google.</summary>
    public bool UseGoogle => IsConfigured && !UseSampleAddresses;
}
