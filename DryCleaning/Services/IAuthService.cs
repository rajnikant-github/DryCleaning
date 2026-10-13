using DryCleaning.Models;

namespace DryCleaning.Services;

public interface IAuthService
{
    Task<AuthResult> LoginAsync(string identifier, string password, UserRole role);
    /// <summary>Sends an OTP and returns the masked destination (e.g. +27 82 *** 4567).</summary>
    Task<string> RequestOtpAsync(string identifier, UserRole role);
    Task<AuthResult> VerifyOtpAsync(string identifier, string code, UserRole role);
    Task LogoutAsync();

    /// <summary>Creates an account. Customers and drivers are signed in; providers get <see cref="AuthResult.PendingApproval"/>.</summary>
    Task<AuthResult> RegisterAsync(Registration registration);

    /// <summary>Sends a password-reset code and returns the masked destination.</summary>
    Task<string> RequestPasswordResetAsync(string identifier);

    /// <summary>Checks the reset code, sets the new password and signs in (other devices are signed out).</summary>
    Task<AuthResult> ResetPasswordAsync(string identifier, string code, string newPassword, UserRole role);

    /// <summary>Changes the signed-in user's password. Throws with a user-friendly message on failure.</summary>
    Task ChangePasswordAsync(string currentPassword, string newPassword);

    /// <summary>Signs back in with the session saved on this phone (API mode). True when a user is signed in.</summary>
    Task<bool> RestoreSessionAsync();

    /// <summary>Code to show on the OTP screen while testing (mock code, or the API's development code). Null in production.</summary>
    string? OtpHint { get; }
}
