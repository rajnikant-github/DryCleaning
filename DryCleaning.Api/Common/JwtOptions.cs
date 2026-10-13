using DryCleaning.Api.Domain;
using Microsoft.EntityFrameworkCore;

namespace DryCleaning.Api.Common;

public sealed class JwtOptions
{
    public const string Section = "Jwt";

    public string Issuer { get; set; } = "EDryCleaners";
    public string Audience { get; set; } = "EDryCleaners.App";
    public string SigningKey { get; set; } = string.Empty;
    /// <summary>Access token lifetime. Keep it short – the app renews it with the refresh token.</summary>
    public int ExpiryMinutes { get; set; } = 60;

    /// <summary>Refresh token lifetime (sliding: every refresh issues a new one).</summary>
    public int RefreshTokenDays { get; set; } = 30;
}
