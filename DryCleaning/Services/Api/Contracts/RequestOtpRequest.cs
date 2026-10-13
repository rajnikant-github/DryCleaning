using DryCleaning.Models;

namespace DryCleaning.Services.Api.Contracts;

/// <summary>POST api/auth/otp/request.</summary>
public sealed class RequestOtpRequest
{
    public string Identifier { get; set; } = string.Empty;
}
