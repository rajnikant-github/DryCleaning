using DryCleaning.Helpers;
using DryCleaning.Models;
using DryCleaning.Services.Api.Contracts;
using DryCleaning.Services.Push;

namespace DryCleaning.Services.Api;

/// <summary>Sign-in against the API: password or OTP, JWT + refresh token stored on the phone.</summary>
public sealed class ApiAuthService : IAuthService
{
    private readonly ApiClient _api;
    private readonly TokenStore _tokens;
    private readonly ISessionService _session;
    private readonly INavigationService _navigation;
    private readonly IRealtimeService _realtime;
    private readonly IPushService _push;

    public ApiAuthService(ApiClient api, TokenStore tokens, ISessionService session, INavigationService navigation, IRealtimeService realtime, IPushService push)
    {
        _api = api;
        _tokens = tokens;
        _session = session;
        _navigation = navigation;
        _realtime = realtime;
        _push = push;
        _api.SessionExpired += OnSessionExpired;
        _api.SessionRefreshed += (_, auth) => SignIn(auth);
    }

    public string? OtpHint { get; private set; }

    public async Task<AuthResult> LoginAsync(string identifier, string password, UserRole role)
    {
        if (string.IsNullOrWhiteSpace(identifier))
            return AuthResult.Fail("Enter your mobile number or email.");
        if (string.IsNullOrWhiteSpace(password))
            return AuthResult.Fail("Enter your password.");

        try
        {
            var auth = await _api.SendAsync(HttpMethod.Post, "api/auth/login",
                new LoginRequest { Identifier = identifier.Trim(), Password = password },
                ApiJsonContext.Default.LoginRequest, ApiJsonContext.Default.AuthResponse, authorize: false);
            return await CompleteSignInAsync(auth, role);
        }
        catch (ApiException ex)
        {
            return AuthResult.Fail(ex.Message);
        }
    }

    public async Task<string> RequestOtpAsync(string identifier, UserRole role)
    {
        OtpHint = null;
        var response = await _api.SendAsync(HttpMethod.Post, "api/auth/otp/request",
            new RequestOtpRequest { Identifier = identifier.Trim() },
            ApiJsonContext.Default.RequestOtpRequest, ApiJsonContext.Default.RequestOtpResponse, authorize: false);
        OtpHint = response.DevCode;
        return response.Destination;
    }

    public async Task<AuthResult> VerifyOtpAsync(string identifier, string code, UserRole role)
    {
        try
        {
            var auth = await _api.SendAsync(HttpMethod.Post, "api/auth/otp/verify",
                new VerifyOtpRequest { Identifier = identifier.Trim(), Code = code.Trim() },
                ApiJsonContext.Default.VerifyOtpRequest, ApiJsonContext.Default.AuthResponse, authorize: false);
            return await CompleteSignInAsync(auth, role);
        }
        catch (ApiException ex)
        {
            return AuthResult.Fail(ex.Message);
        }
    }

    public async Task<AuthResult> RegisterAsync(Registration registration)
    {
        var json = ApiJsonContext.Default;
        var email = Clean(registration.Email);
        var phone = Clean(registration.Phone);
        try
        {
            var auth = registration.Role switch
            {
                UserRole.Customer => await _api.SendAsync(HttpMethod.Post, "api/auth/register/customer",
                    new RegisterCustomerRequest { Name = registration.Name.Trim(), Email = email, Phone = phone, Password = registration.Password },
                    json.RegisterCustomerRequest, json.AuthResponse, authorize: false),
                UserRole.Driver => await _api.SendAsync(HttpMethod.Post, "api/auth/register/driver",
                    new RegisterDriverRequest
                    {
                        Name = registration.Name.Trim(), Email = email, Phone = phone, Password = registration.Password,
                        LicenseNumber = registration.LicenseNumber?.Trim() ?? string.Empty
                    },
                    json.RegisterDriverRequest, json.AuthResponse, authorize: false),
                UserRole.Provider => await _api.SendAsync(HttpMethod.Post, "api/auth/register/provider",
                    new RegisterProviderRequest
                    {
                        Name = registration.Name.Trim(), Email = email, Phone = phone, Password = registration.Password,
                        BusinessName = registration.BusinessName?.Trim() ?? string.Empty,
                        Description = Clean(registration.Description),
                        CategoryIds = registration.CategoryIds.Distinct().ToList()
                    },
                    json.RegisterProviderRequest, json.AuthResponse, authorize: false),
                _ => throw new ApiException("Only Customer, Service Provider and Driver accounts can be created in the app.")
            };

            if (auth.User.Role == UserRole.Provider)
            {
                // New businesses are reviewed by an admin first – don't keep a session for them yet.
                if (!string.IsNullOrEmpty(auth.RefreshToken))
                    await RevokeAsync(auth.RefreshToken);
                return AuthResult.Pending(PendingApprovalMessage);
            }

            await _tokens.SaveAsync(auth);
            SignIn(auth);
            _ = _push.EnableAsync();
            return AuthResult.Ok();
        }
        catch (ApiException ex)
        {
            return AuthResult.Fail(ex.Message);
        }
    }

    public async Task<string> RequestPasswordResetAsync(string identifier)
    {
        OtpHint = null;
        var response = await _api.SendAsync(HttpMethod.Post, "api/auth/password/forgot",
            new ForgotPasswordRequest { Identifier = identifier.Trim() },
            ApiJsonContext.Default.ForgotPasswordRequest, ApiJsonContext.Default.RequestOtpResponse, authorize: false);
        OtpHint = response.DevCode;
        return response.Destination;
    }

    public async Task<AuthResult> ResetPasswordAsync(string identifier, string code, string newPassword, UserRole role)
    {
        try
        {
            var auth = await _api.SendAsync(HttpMethod.Post, "api/auth/password/reset",
                new ResetPasswordRequest { Identifier = identifier.Trim(), Code = code.Trim(), NewPassword = newPassword },
                ApiJsonContext.Default.ResetPasswordRequest, ApiJsonContext.Default.AuthResponse, authorize: false);
            return await CompleteSignInAsync(auth, role);
        }
        catch (ApiException ex)
        {
            return AuthResult.Fail(ex.Message);
        }
    }

    public Task ChangePasswordAsync(string currentPassword, string newPassword) =>
        _api.SendAsync(HttpMethod.Post, "api/auth/me/change-password",
            new ChangePasswordRequest { CurrentPassword = currentPassword, NewPassword = newPassword },
            ApiJsonContext.Default.ChangePasswordRequest);

    /// <summary>Shown after a Service Provider signs up (the business needs admin approval).</summary>
    public const string PendingApprovalMessage =
        "Thanks for registering! Your business is being reviewed by the EDry Cleaners team. We'll let you know as soon as it's approved – then you can sign in.";

    private static string? Clean(string? value) => string.IsNullOrWhiteSpace(value) ? null : value.Trim();

    public async Task LogoutAsync()
    {
        var auth = await _tokens.GetAsync();
        if (!string.IsNullOrEmpty(auth?.RefreshToken))
            await RevokeAsync(auth.RefreshToken);

        await _push.DisableAsync(); // while the session is still valid
        await _realtime.StopAsync();
        _tokens.Clear();
        _session.SignOut();
    }

    public async Task<bool> RestoreSessionAsync()
    {
        var saved = await _tokens.GetAsync();
        if (saved is null || string.IsNullOrEmpty(saved.RefreshToken))
            return false;

        if (saved.RefreshTokenExpiresAt is { } expires && ApiMapping.Local(expires) <= DateTime.Now)
        {
            _tokens.Clear();
            return false;
        }

        try
        {
            saved = await _api.RefreshSessionAsync();
        }
        catch (SessionExpiredException)
        {
            return false;
        }
        catch (ApiException)
        {
            // Offline or API down: open the app with the saved session; screens show their offline/error state.
        }

        SignIn(saved);
        _ = _push.EnableAsync();
        return true;
    }

    private async Task<AuthResult> CompleteSignInAsync(AuthResponse auth, UserRole selectedRole)
    {
        if (auth.User.Role != selectedRole)
        {
            if (!string.IsNullOrEmpty(auth.RefreshToken))
                await RevokeAsync(auth.RefreshToken);

            return AuthResult.Fail(auth.User.Role == UserRole.Admin
                ? "Admin accounts sign in on the EDry Cleaners admin portal."
                : $"This is a {RoleName(auth.User.Role)} account. Go back and choose \"{RoleName(auth.User.Role)}\" to sign in.");
        }

        await _tokens.SaveAsync(auth);
        SignIn(auth);
        _ = _push.EnableAsync();
        return AuthResult.Ok();
    }

    private void SignIn(AuthResponse auth)
    {
        _session.SignIn(ApiMapping.ToUser(auth.User), auth.User.ProfileId);
        _ = _realtime.StartAsync(); // no-op when already connected
    }

    private async Task RevokeAsync(string refreshToken)
    {
        try
        {
            await _api.SendAsync(HttpMethod.Post, "api/auth/logout",
                new RefreshTokenRequest { RefreshToken = refreshToken }, ApiJsonContext.Default.RefreshTokenRequest, authorize: false);
        }
        catch (ApiException)
        {
            // Signing out must always work, even offline. The token expires on its own.
        }
    }

    private void OnSessionExpired(object? sender, EventArgs e)
    {
        if (!_session.IsSignedIn)
            return;

        _session.SignOut();
        _ = _realtime.StopAsync();
        MainThread.BeginInvokeOnMainThread(async () =>
        {
            try
            {
                await _navigation.GoToAsync(Routes.RoleSelection);
            }
            catch (Exception)
            {
                // Shell not ready (e.g. during start-up) – the splash screen handles routing.
            }
        });
    }

    private static string RoleName(UserRole role) => role switch
    {
        UserRole.Provider => "Service Provider",
        _ => role.ToString()
    };
}
