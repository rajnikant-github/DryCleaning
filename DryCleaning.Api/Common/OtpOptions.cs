using DryCleaning.Api.Domain;
using Microsoft.EntityFrameworkCore;

namespace DryCleaning.Api.Common;

public sealed class OtpOptions
{
    public const string Section = "Otp";

    public int Length { get; set; } = 6;
    public int ExpirySeconds { get; set; } = 300;

    /// <summary>Development only: return the code in the API response instead of sending an SMS.</summary>
    public bool ExposeCodeInResponse { get; set; }

    /// <summary>Wrong guesses allowed before a code is invalidated (login and password reset).</summary>
    public int MaxAttempts { get; set; } = 5;
}
