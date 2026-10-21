using DryCleaning.Api.Common;
using DryCleaning.Api.Dtos;
using DryCleaning.Api.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace DryCleaning.Api.Controllers;

/// <summary>Registration, login (password or OTP) and the signed-in user's account.</summary>
[ApiController]
[Route("api/auth")]
[Produces("application/json")]
public sealed class AuthController(AuthService auth, RegistrationService registration) : ControllerBase
{
    /// <summary>
    /// Sign-up step 1 (customer, provider or driver). Nothing is created yet: the details are held and a 6-digit code is emailed.
    /// Finish with <c>register/confirm</c>.
    /// </summary>
    [HttpPost("register/start")]
    [AllowAnonymous]
    [ProducesResponseType<RegisterStartResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict)]
    public Task<RegisterStartResponse> RegisterStart(RegisterStartRequest request, CancellationToken ct) => registration.StartAsync(request, ct);

    /// <summary>Email a new code for a sign-up that is waiting for its code (one per minute, a few per sign-up).</summary>
    [HttpPost("register/resend")]
    [AllowAnonymous]
    [ProducesResponseType<RegisterStartResponse>(StatusCodes.Status200OK)]
    public Task<RegisterStartResponse> RegisterResend(RegisterResendRequest request, CancellationToken ct) => registration.ResendAsync(request, ct);

    /// <summary>
    /// Sign-up step 2: check the code and create the account. Customers and drivers are signed in;
    /// a provider's business stays inactive until an admin approves it.
    /// </summary>
    [HttpPost("register/confirm")]
    [AllowAnonymous]
    [ProducesResponseType<AuthResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status401Unauthorized)]
    public Task<AuthResponse> RegisterConfirm(RegisterConfirmRequest request, CancellationToken ct) => registration.ConfirmAsync(request, ct);

    /// <summary>Log in with email and password.</summary>
    [HttpPost("login")]
    [AllowAnonymous]
    [ProducesResponseType<AuthResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status401Unauthorized)]
    public Task<AuthResponse> Login(LoginRequest request, CancellationToken ct) => auth.LoginAsync(request, ct);

    /// <summary>Get a new access token with the refresh token (the refresh token rotates – store the new one).</summary>
    [HttpPost("refresh")]
    [AllowAnonymous]
    [ProducesResponseType<AuthResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status401Unauthorized)]
    public Task<AuthResponse> Refresh(RefreshTokenRequest request, CancellationToken ct) => auth.RefreshAsync(request, ct);

    /// <summary>Sign this device out: revokes its refresh token. Also call DELETE /api/me/devices for push.</summary>
    [HttpPost("logout")]
    [AllowAnonymous]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> Logout(RefreshTokenRequest request, CancellationToken ct)
    {
        await auth.LogoutAsync(request, ct);
        return NoContent();
    }

    /// <summary>Forgot password: sends a reset code. Same answer whether or not the account exists.</summary>
    [HttpPost("password/forgot")]
    [AllowAnonymous]
    [ProducesResponseType<RequestOtpResponse>(StatusCodes.Status200OK)]
    public Task<RequestOtpResponse> ForgotPassword(ForgotPasswordRequest request, CancellationToken ct) => auth.ForgotPasswordAsync(request, ct);

    /// <summary>Reset password with the code, then sign in. Signs out the user's other devices.</summary>
    [HttpPost("password/reset")]
    [AllowAnonymous]
    [ProducesResponseType<AuthResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status400BadRequest)]
    public Task<AuthResponse> ResetPassword(ResetPasswordRequest request, CancellationToken ct) => auth.ResetPasswordAsync(request, ct);

    /// <summary>Send a 6-digit login code ("Continue with OTP").</summary>
    [HttpPost("otp/request")]
    [AllowAnonymous]
    [ProducesResponseType<RequestOtpResponse>(StatusCodes.Status200OK)]
    public Task<RequestOtpResponse> RequestOtp(RequestOtpRequest request, CancellationToken ct) => auth.RequestOtpAsync(request, ct);

    /// <summary>Verify the code and sign in.</summary>
    [HttpPost("otp/verify")]
    [AllowAnonymous]
    [ProducesResponseType<AuthResponse>(StatusCodes.Status200OK)]
    public Task<AuthResponse> VerifyOtp(VerifyOtpRequest request, CancellationToken ct) => auth.VerifyOtpAsync(request, ct);

    /// <summary>The signed-in user.</summary>
    [HttpGet("me")]
    [Authorize]
    public Task<UserDto> Me(CancellationToken ct) => auth.MeAsync(ct);

    /// <summary>Update name, email or mobile number.</summary>
    [HttpPut("me")]
    [Authorize]
    public Task<UserDto> UpdateMe(UpdateProfileRequest request, CancellationToken ct) => auth.UpdateMeAsync(request, ct);

    /// <summary>Change password.</summary>
    [HttpPost("me/change-password")]
    [Authorize]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> ChangePassword(ChangePasswordRequest request, CancellationToken ct)
    {
        await auth.ChangePasswordAsync(request, ct);
        return NoContent();
    }
}
