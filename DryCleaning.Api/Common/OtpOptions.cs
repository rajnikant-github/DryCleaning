using DryCleaning.Api.Domain;
using Microsoft.EntityFrameworkCore;

namespace DryCleaning.Api.Common;

public sealed class OtpOptions
{
    public const string Section = "Otp";

    public int Length { get; set; } = 6;
    public int ExpirySeconds { get; set; } = 300;


    /// <summary>Wrong guesses allowed before a code is invalidated (login and password reset).</summary>
    public int MaxAttempts { get; set; } = 5;
}
