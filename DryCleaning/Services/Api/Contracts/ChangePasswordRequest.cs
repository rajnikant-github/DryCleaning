using DryCleaning.Models;

namespace DryCleaning.Services.Api.Contracts;

/// <summary>POST api/auth/me/change-password.</summary>
public sealed class ChangePasswordRequest
{
    public string CurrentPassword { get; set; } = string.Empty;
    public string NewPassword { get; set; } = string.Empty;
}
