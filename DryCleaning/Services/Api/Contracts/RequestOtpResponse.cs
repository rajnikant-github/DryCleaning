using DryCleaning.Models;

namespace DryCleaning.Services.Api.Contracts;

/// <summary>Response of api/auth/otp/request.</summary>
public sealed class RequestOtpResponse
{
    public string Destination { get; set; } = string.Empty;
    public int ExpiresInSeconds { get; set; }
    public string? DevCode { get; set; }
}
