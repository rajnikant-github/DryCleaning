using DryCleaning.Models;

namespace DryCleaning.Services;

public interface IAuthService
{
    Task<AuthResult> LoginAsync(string identifier, string password, UserRole role);
    /// <summary>Sends an OTP and returns the masked destination (e.g. +27 82 *** 4567).</summary>
    Task<string> RequestOtpAsync(string identifier, UserRole role);
    Task<AuthResult> VerifyOtpAsync(string identifier, string code, UserRole role);
    Task LogoutAsync();

    /// <summary>Signs back in with the session saved on this phone (API mode). True when a user is signed in.</summary>
    Task<bool> RestoreSessionAsync();

    /// <summary>Code to show on the OTP screen while testing (mock code, or the API's development code). Null in production.</summary>
    string? OtpHint { get; }
}
