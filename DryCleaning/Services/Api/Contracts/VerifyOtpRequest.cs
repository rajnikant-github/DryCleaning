using DryCleaning.Models;

namespace DryCleaning.Services.Api.Contracts;

/// <summary>POST api/auth/otp/verify.</summary>
public sealed class VerifyOtpRequest
{
    public string Identifier { get; set; } = string.Empty;
    public string Code { get; set; } = string.Empty;
}
