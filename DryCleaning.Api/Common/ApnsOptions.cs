namespace DryCleaning.Api.Common;

/// <summary>Section "ApnsSettings": Apple Push Notification service for iPhones (token-based auth with the .p8 key).</summary>
public sealed class ApnsOptions
{
    public const string Section = "ApnsSettings";

    public string? TeamId { get; set; }
    public string? KeyId { get; set; }

    /// <summary>The app's bundle id, sent as the apns-topic.</summary>
    public string? BundleId { get; set; }

    /// <summary>Path to the AuthKey_XXXX.p8 file (relative paths start at the API folder). Keep it out of git.</summary>
    public string? PrivateKeyPath { get; set; }

    /// <summary>True for development builds (Xcode/TestFlight-internal debug), false for TestFlight/App Store builds.</summary>
    public bool UseSandbox { get; set; }

    public bool IsConfigured =>
        !string.IsNullOrWhiteSpace(TeamId) && !string.IsNullOrWhiteSpace(KeyId) &&
        !string.IsNullOrWhiteSpace(BundleId) && !string.IsNullOrWhiteSpace(PrivateKeyPath);
}
