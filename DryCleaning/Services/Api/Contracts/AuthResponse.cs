using DryCleaning.Models;

namespace DryCleaning.Services.Api.Contracts;

/// <summary>Sign-in result (login, OTP, refresh).</summary>
public sealed class AuthResponse
{
    public string AccessToken { get; set; } = string.Empty;
    public DateTime ExpiresAt { get; set; }
    public UserDto User { get; set; } = new();
    public string? RefreshToken { get; set; }
    public DateTime? RefreshTokenExpiresAt { get; set; }
}
