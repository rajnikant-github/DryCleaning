namespace DryCleaning.Api.Common;

/// <summary>Section "Verification": limits for the identity and business documents providers and drivers upload.</summary>
public sealed class VerificationOptions
{
    public const string Section = "Verification";

    /// <summary>
    /// Largest PDF accepted: 50 MB by default. Change it with <c>Verification:MaxPdfBytes</c> (or the
    /// <c>Verification__MaxPdfBytes</c> environment variable). A reverse proxy in front of the API (nginx, IIS, App Service) has its own
    /// upload limit, which must be at least this big.
    /// </summary>
    public long MaxPdfBytes { get; set; } = 50 * 1024 * 1024;

    /// <summary>
    /// Largest photo accepted: 15 MB by default. The app shrinks photos to a clearly readable size before sending them, so this is only a
    /// safety net for other clients. Change it with <c>Verification:MaxImageBytes</c>.
    /// </summary>
    public long MaxImageBytes { get; set; } = 15 * 1024 * 1024;

    /// <summary>How long a browser preview link for an uploaded document works (seconds).</summary>
    public int PreviewLinkSeconds { get; set; } = 120;

    /// <summary>
    /// Months after which documents are deleted once their application was rejected (never fixed) or the account was deactivated.
    /// 0 turns the clean-up off.
    /// </summary>
    public int RetentionMonths { get; set; } = 12;
}
