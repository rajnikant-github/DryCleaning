using DryCleaning.Helpers;
using DryCleaning.Models;
using DryCleaning.Services.Api.Contracts;

namespace DryCleaning.Services.Api;

/// <summary>Sign-in against the API: password or OTP, JWT + refresh token stored on the phone.</summary>
public sealed class ApiAuthService : IAuthService
{
    private readonly ApiClient _api;
    private readonly TokenStore _tokens;
    private readonly ISessionService _session;
    private readonly INavigationService _navigation;

    public ApiAuthService(ApiClient api, TokenStore tokens, ISessionService session, INavigationService navigation)
    {
        _api = api;
        _tokens = tokens;
        _session = session;
        _navigation = navigation;
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

    public async Task LogoutAsync()
    {
        var auth = await _tokens.GetAsync();
        if (!string.IsNullOrEmpty(auth?.RefreshToken))
            await RevokeAsync(auth.RefreshToken);

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
        return AuthResult.Ok();
    }

    private void SignIn(AuthResponse auth) => _session.SignIn(ApiMapping.ToUser(auth.User), auth.User.ProfileId);

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
