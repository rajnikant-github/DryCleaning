using System.Diagnostics;
using System.Text.Json;
using DryCleaning.Services.Api.Contracts;

namespace DryCleaning.Services.Api;

/// <summary>
/// Keeps the signed-in session (access token, refresh token and user) in SecureStorage so the app
/// stays signed in across restarts. Cached in memory after the first read.
/// </summary>
public sealed class TokenStore
{
    private const string StorageKey = "edc.auth.session";

    private AuthResponse? _current;
    private bool _loaded;

    public async Task<AuthResponse?> GetAsync()
    {
        if (_loaded)
            return _current;

        try
        {
            var json = await SecureStorage.Default.GetAsync(StorageKey);
            _current = string.IsNullOrEmpty(json) ? null : JsonSerializer.Deserialize(json, ApiJsonContext.Default.AuthResponse);
        }
        catch (Exception ex)
        {
            // SecureStorage can fail after an app reinstall/restore – treat as signed out.
            Debug.WriteLine($"[TokenStore] Could not read session: {ex.Message}");
            _current = null;
        }

        _loaded = true;
        return _current;
    }

    public async Task SaveAsync(AuthResponse auth)
    {
        // A refresh response always carries a new refresh token; keep the old one only if it doesn't.
        if (string.IsNullOrEmpty(auth.RefreshToken) && _current is not null)
        {
            auth.RefreshToken = _current.RefreshToken;
            auth.RefreshTokenExpiresAt = _current.RefreshTokenExpiresAt;
        }

        _current = auth;
        _loaded = true;

        try
        {
            await SecureStorage.Default.SetAsync(StorageKey, JsonSerializer.Serialize(auth, ApiJsonContext.Default.AuthResponse));
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"[TokenStore] Could not save session: {ex.Message}");
        }
    }

    public void Clear()
    {
        _current = null;
        _loaded = true;

        try
        {
            SecureStorage.Default.Remove(StorageKey);
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"[TokenStore] Could not clear session: {ex.Message}");
        }
    }
}
