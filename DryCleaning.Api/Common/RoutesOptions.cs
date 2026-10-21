namespace DryCleaning.Api.Common;

/// <summary>Section "Routes": Google Routes API, used to measure the real driving distance for the pickup &amp; delivery fee.</summary>
public sealed class RoutesOptions
{
    public const string Section = "Routes";

    public bool Enabled { get; set; } = true;

    /// <summary>Server key restricted to the Routes API (kept in secrets.json). Empty = the distance is estimated instead.</summary>
    public string? ApiKey { get; set; }

    /// <summary>How long a measured route is remembered (addresses rarely move, so this saves paid Google calls).</summary>
    public int CacheDays { get; set; } = 14;

    public bool IsConfigured => Enabled && !string.IsNullOrWhiteSpace(ApiKey);
}
