using DryCleaning.Models;

namespace DryCleaning.Services;

public interface IAuthService
{
    Task<AuthResult> LoginAsync(string identifier, string password, UserRole role);
    /// <summary>Sends an OTP and returns the masked destination (e.g. +27 82 *** 4567).</summary>
    Task<string> RequestOtpAsync(string identifier, UserRole role);
    Task<AuthResult> VerifyOtpAsync(string identifier, string code, UserRole role);
    Task LogoutAsync();

    /// <summary>Sign-up step 1: checks the details and emails a code. No account exists yet.</summary>
    Task<RegistrationStart> StartRegistrationAsync(Registration registration);

    /// <summary>Emails a new sign-up code and returns the masked email.</summary>
    Task<RegistrationStart> ResendRegistrationCodeAsync(string email);

    /// <summary>Sign-up step 2: checks the code and creates the account. The user is signed in; a provider or driver lands on the "under review" screen until an admin approves them.</summary>
    Task<AuthResult> ConfirmRegistrationAsync(string email, string code);

    /// <summary>Sends a password-reset code and returns the masked destination.</summary>
    Task<string> RequestPasswordResetAsync(string identifier);

    /// <summary>Checks the reset code, sets the new password and signs in (other devices are signed out).</summary>
    Task<AuthResult> ResetPasswordAsync(string identifier, string code, string newPassword, UserRole role);

    /// <summary>Changes the signed-in user's password. Throws with a user-friendly message on failure.</summary>
    Task ChangePasswordAsync(string currentPassword, string newPassword);

    /// <summary>Asks the API for the signed-in user's current status (e.g. approved yet?) and updates the session. Returns the fresh user, or null when offline.</summary>
    Task<User?> RefreshAccountAsync();

    /// <summary>Signs back in with the session saved on this phone (API mode). True when a user is signed in.</summary>
    Task<bool> RestoreSessionAsync();

}
