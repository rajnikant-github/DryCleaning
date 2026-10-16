using System.Diagnostics;
using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization.Metadata;
using DryCleaning.Configuration;
using DryCleaning.Models;
using DryCleaning.Services.Api.Contracts;

namespace DryCleaning.Services.Api;

/// <summary>
/// The one place the app talks HTTP to the EDry Cleaners API. Adds the bearer token, refreshes it
/// when it is about to expire (or the API answers 401), and turns error responses into
/// <see cref="ApiException"/>s with messages that can be shown to the user.
/// </summary>
public sealed class ApiClient
{
    private static readonly TimeSpan RefreshMargin = TimeSpan.FromSeconds(60);

    private readonly HttpClient _http;
    private readonly TokenStore _tokens;
    private readonly IConnectivity _connectivity;
    private readonly SemaphoreSlim _refreshGate = new(1, 1);

    public ApiClient(AppSettings settings, TokenStore tokens, IConnectivity connectivity)
    {
        _tokens = tokens;
        _connectivity = connectivity;
        _http = new HttpClient
        {
            BaseAddress = new Uri(settings.Api.ResolveBaseUrl()),
            Timeout = TimeSpan.FromSeconds(Math.Max(5, settings.Api.TimeoutSeconds))
        };
        _http.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
    }

    /// <summary>Raised when the refresh token is rejected – the user must sign in again.</summary>
    public event EventHandler? SessionExpired;

    /// <summary>Raised after the tokens were refreshed (carries the latest user details).</summary>
    public event EventHandler<AuthResponse>? SessionRefreshed;

    public Uri BaseAddress => _http.BaseAddress!;

    // ── Typed helpers ────────────────────────────────────────────────────────

    public async Task<T> GetAsync<T>(string path, JsonTypeInfo<T> resultType, CancellationToken ct = default, bool authorize = true)
    {
        using var response = await SendAsync(() => new HttpRequestMessage(HttpMethod.Get, path), authorize, ct);
        return await ReadAsync(response, resultType, ct);
    }

    /// <summary>GET that returns null for 204 No Content and 404 Not Found.</summary>
    public async Task<T?> GetOrDefaultAsync<T>(string path, JsonTypeInfo<T> resultType, CancellationToken ct = default) where T : class
    {
        try
        {
            using var response = await SendAsync(() => new HttpRequestMessage(HttpMethod.Get, path), authorize: true, ct);
            if (response.StatusCode == HttpStatusCode.NoContent)
                return null;
            return await ReadAsync(response, resultType, ct);
        }
        catch (ApiException ex) when (ex.IsNotFound)
        {
            return null;
        }
    }

    public async Task<TResult> SendAsync<TBody, TResult>(
        HttpMethod method, string path, TBody body, JsonTypeInfo<TBody> bodyType, JsonTypeInfo<TResult> resultType,
        CancellationToken ct = default, bool authorize = true)
    {
        using var response = await SendAsync(() => new HttpRequestMessage(method, path) { Content = JsonContent.Create(body, bodyType) }, authorize, ct);
        return await ReadAsync(response, resultType, ct);
    }

    public async Task SendAsync<TBody>(HttpMethod method, string path, TBody body, JsonTypeInfo<TBody> bodyType, CancellationToken ct = default, bool authorize = true)
    {
        using var response = await SendAsync(() => new HttpRequestMessage(method, path) { Content = JsonContent.Create(body, bodyType) }, authorize, ct);
    }

    /// <summary>POST without a body (e.g. accept / quality-check), reading the result.</summary>
    public async Task<TResult> PostAsync<TResult>(string path, JsonTypeInfo<TResult> resultType, CancellationToken ct = default)
    {
        using var response = await SendAsync(() => new HttpRequestMessage(HttpMethod.Post, path), authorize: true, ct);
        return await ReadAsync(response, resultType, ct);
    }

    /// <summary>POST without a body, ignoring the result.</summary>
    public async Task PostAsync(string path, CancellationToken ct = default)
    {
        using var response = await SendAsync(() => new HttpRequestMessage(HttpMethod.Post, path), authorize: true, ct);
    }

    /// <summary>Uploads local photos to an order (POST api/orders/{id}/photos, multipart).</summary>
    public async Task UploadPhotosAsync(Guid orderId, PhotoType type, IEnumerable<string>? filePaths, CancellationToken ct = default)
    {
        if (filePaths is null)
            return;

        foreach (var path in filePaths.Where(p => !string.IsNullOrWhiteSpace(p) && File.Exists(p)))
        {
            using var response = await SendAsync(() =>
            {
                var file = new StreamContent(File.OpenRead(path));
                file.Headers.ContentType = new MediaTypeHeaderValue(ContentTypeFor(path));
                var form = new MultipartFormDataContent
                {
                    { file, "File", Path.GetFileName(path) },
                    { new StringContent(type.ToString()), "Type" }
                };
                return new HttpRequestMessage(HttpMethod.Post, $"api/orders/{orderId}/photos") { Content = form };
            }, authorize: true, ct);
        }
    }

    /// <summary>Uploads one local file as multipart field "File" and reads the result.</summary>
    public async Task<TResult> UploadFileAsync<TResult>(string path, string filePath, JsonTypeInfo<TResult> resultType, CancellationToken ct = default)
    {
        if (!File.Exists(filePath))
            throw new ApiException("The photo could not be found on this phone. Please choose it again.");

        using var response = await SendAsync(() =>
        {
            var file = new StreamContent(File.OpenRead(filePath));
            file.Headers.ContentType = new MediaTypeHeaderValue(ContentTypeFor(filePath));
            var form = new MultipartFormDataContent { { file, "File", Path.GetFileName(filePath) } };
            return new HttpRequestMessage(HttpMethod.Post, path) { Content = form };
        }, authorize: true, ct);
        return await ReadAsync(response, resultType, ct);
    }

    /// <summary>Reverse of <see cref="AbsoluteUrl"/>: an address on this API back to "/uploads/x.jpg" before sending it.</summary>
    public string? RelativeUrl(string? url)
    {
        if (string.IsNullOrWhiteSpace(url))
            return null;
        var root = BaseAddress.ToString();
        return url.StartsWith(root, StringComparison.OrdinalIgnoreCase) ? "/" + url[root.Length..] : url;
    }

    /// <summary>Turns "/uploads/x.jpg" from the API into a full URL the app can load.</summary>
    public string AbsoluteUrl(string url) =>
        string.IsNullOrWhiteSpace(url) || Uri.IsWellFormedUriString(url, UriKind.Absolute)
            ? url
            : new Uri(BaseAddress, url.TrimStart('/')).ToString();

    // ── Session ──────────────────────────────────────────────────────────────

    /// <summary>Exchanges the stored refresh token for a new session. Throws <see cref="SessionExpiredException"/> when it is no longer valid.</summary>
    public Task<AuthResponse> RefreshSessionAsync(CancellationToken ct = default) => RefreshAsync(staleAccessToken: null, ct);

    /// <summary>A valid access token (refreshed first when it is about to expire), or null when signed out. Used by the real-time connection.</summary>
    public async Task<string?> GetAccessTokenAsync(CancellationToken ct = default)
    {
        var auth = await _tokens.GetAsync();
        if (auth is null)
            return null;
        if (AsUtc(auth.ExpiresAt) <= DateTime.UtcNow.Add(RefreshMargin))
            auth = await RefreshAsync(auth.AccessToken, ct);
        return auth.AccessToken;
    }

    private async Task AttachTokenAsync(HttpRequestMessage request, bool forceRefresh, CancellationToken ct)
    {
        var auth = await _tokens.GetAsync() ?? throw new SessionExpiredException();
        if (forceRefresh || AsUtc(auth.ExpiresAt) <= DateTime.UtcNow.Add(RefreshMargin))
            auth = await RefreshAsync(auth.AccessToken, ct);

        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", auth.AccessToken);
    }

    private async Task<AuthResponse> RefreshAsync(string? staleAccessToken, CancellationToken ct)
    {
        await _refreshGate.WaitAsync(ct);
        try
        {
            var current = await _tokens.GetAsync();
            if (current is null || string.IsNullOrEmpty(current.RefreshToken))
                throw Expire();

            // Another request refreshed while we were waiting.
            if (staleAccessToken is not null && current.AccessToken != staleAccessToken &&
                AsUtc(current.ExpiresAt) > DateTime.UtcNow.Add(RefreshMargin))
                return current;

            using var request = new HttpRequestMessage(HttpMethod.Post, "api/auth/refresh")
            {
                Content = JsonContent.Create(new RefreshTokenRequest { RefreshToken = current.RefreshToken }, ApiJsonContext.Default.RefreshTokenRequest)
            };
            using var response = await SendRawAsync(request, ct);
            if (response.StatusCode is HttpStatusCode.Unauthorized or HttpStatusCode.BadRequest or HttpStatusCode.Forbidden)
                throw Expire();
            if (!response.IsSuccessStatusCode)
                throw await ToExceptionAsync(response, ct);

            var refreshed = await ReadAsync(response, ApiJsonContext.Default.AuthResponse, ct);
            await _tokens.SaveAsync(refreshed);
            SessionRefreshed?.Invoke(this, refreshed);
            return refreshed;
        }
        finally
        {
            _refreshGate.Release();
        }
    }

    private SessionExpiredException Expire()
    {
        _tokens.Clear();
        SessionExpired?.Invoke(this, EventArgs.Empty);
        return new SessionExpiredException();
    }

    // ── Plumbing ─────────────────────────────────────────────────────────────

    /// <summary>Sends a request (built by <paramref name="create"/> so it can be re-sent after a token refresh).</summary>
    private async Task<HttpResponseMessage> SendAsync(Func<HttpRequestMessage> create, bool authorize, CancellationToken ct)
    {
        if (_connectivity.NetworkAccess == Microsoft.Maui.Networking.NetworkAccess.None)
            throw new ApiException("You're offline. Check your connection and try again.");

        using var request = create();
        if (authorize)
            await AttachTokenAsync(request, forceRefresh: false, ct);

        var response = await SendRawAsync(request, ct);
        if (authorize && response.StatusCode == HttpStatusCode.Unauthorized)
        {
            response.Dispose();
            using var retry = create();
            await AttachTokenAsync(retry, forceRefresh: true, ct);
            response = await SendRawAsync(retry, ct);
            if (response.StatusCode == HttpStatusCode.Unauthorized)
            {
                response.Dispose();
                throw Expire();
            }
        }

        if (response.IsSuccessStatusCode)
            return response;

        var error = await ToExceptionAsync(response, ct);
        response.Dispose();
        throw error;
    }

    private async Task<HttpResponseMessage> SendRawAsync(HttpRequestMessage request, CancellationToken ct)
    {
        try
        {
            return await _http.SendAsync(request, ct);
        }
        catch (TaskCanceledException ex) when (!ct.IsCancellationRequested)
        {
            throw new ApiException("EDry Cleaners is taking too long to respond. Please try again.", inner: ex);
        }
        catch (HttpRequestException ex)
        {
            Debug.WriteLine($"[ApiClient] {request.Method} {request.RequestUri} failed: {ex.Message}");
            throw new ApiException("Can't reach EDry Cleaners right now. Check your connection and try again.", inner: ex);
        }
    }

    private static async Task<T> ReadAsync<T>(HttpResponseMessage response, JsonTypeInfo<T> type, CancellationToken ct)
    {
        try
        {
            var value = await response.Content.ReadFromJsonAsync(type, ct);
            return value ?? throw new ApiException("The server sent an empty response.", response.StatusCode);
        }
        catch (JsonException ex)
        {
            Debug.WriteLine($"[ApiClient] Could not read {typeof(T).Name}: {ex.Message}");
            throw new ApiException("The server sent a response the app doesn't understand. Please update the app.", response.StatusCode, ex);
        }
    }

    private static async Task<ApiException> ToExceptionAsync(HttpResponseMessage response, CancellationToken ct)
    {
        ProblemDetailsDto? problem = null;
        try
        {
            if (response.Content.Headers.ContentLength is not 0)
                problem = await response.Content.ReadFromJsonAsync(ApiJsonContext.Default.ProblemDetailsDto, ct);
        }
        catch (Exception)
        {
            // Not a ProblemDetails body – fall back to a generic message.
        }

        var message = problem?.Errors is { Count: > 0 } errors
            ? string.Join(Environment.NewLine, errors.SelectMany(e => e.Value).Distinct())
            : problem?.Detail ?? problem?.Title;

        if (string.IsNullOrWhiteSpace(message) || response.StatusCode >= HttpStatusCode.InternalServerError)
        {
            message = response.StatusCode switch
            {
                HttpStatusCode.NotFound => "We couldn't find that. It may have been removed.",
                HttpStatusCode.Forbidden => "Your account doesn't have access to this.",
                HttpStatusCode.Unauthorized => "Please sign in again.",
                HttpStatusCode.Conflict => "That can't be done right now. Refresh and try again.",
                >= HttpStatusCode.InternalServerError => "Something went wrong on our side. Please try again in a moment.",
                _ => "That didn't work. Please try again."
            };
        }

        return new ApiException(message, response.StatusCode);
    }

    private static DateTime AsUtc(DateTime value) => value.Kind switch
    {
        DateTimeKind.Utc => value,
        DateTimeKind.Local => value.ToUniversalTime(),
        _ => DateTime.SpecifyKind(value, DateTimeKind.Utc)
    };

    private static string ContentTypeFor(string path) => Path.GetExtension(path).ToLowerInvariant() switch
    {
        ".png" => "image/png",
        ".heic" => "image/heic",
        ".webp" => "image/webp",
        _ => "image/jpeg"
    };
}
