using DryCleaning.Models;

namespace DryCleaning.Services;

public interface IAuthService
{
    Task<AuthResult> LoginAsync(string identifier, string password, UserRole role);
    /// <summary>Sends an OTP and returns the masked destination (e.g. +27 82 *** 4567).</summary>
    Task<string> RequestOtpAsync(string identifier, UserRole role);
    Task<AuthResult> VerifyOtpAsync(string identifier, string code, UserRole role);
    Task LogoutAsync();
}
