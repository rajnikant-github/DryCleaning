using DryCleaning.Models;

namespace DryCleaning.Services.Api.Contracts;

/// <summary>POST api/auth/password/forgot.</summary>
public sealed class ForgotPasswordRequest
{
    public string Identifier { get; set; } = string.Empty;
}
