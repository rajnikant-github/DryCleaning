using DryCleaning.Models;

namespace DryCleaning.Services.Api.Contracts;

/// <summary>POST api/auth/password/reset.</summary>
public sealed class ResetPasswordRequest
{
    public string Identifier { get; set; } = string.Empty;
    public string Code { get; set; } = string.Empty;
    public string NewPassword { get; set; } = string.Empty;
}
