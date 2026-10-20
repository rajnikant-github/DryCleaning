using System.Net;
using System.Net.Http.Headers;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using DryCleaning.Api.Common;
using DryCleaning.Api.Domain;

namespace DryCleaning.Api.Infrastructure.Push;

/// <summary>
/// Apple Push Notification service over HTTP/2 with token-based authentication (the .p8 key):
/// a short-lived ES256 JWT signed with the key identifies this server to Apple.
/// </summary>
public sealed class ApnsPushSender : IPushSender, IDisposable
{
    private static readonly TimeSpan JwtLifetime = TimeSpan.FromMinutes(50); // Apple accepts 20–60 minutes

    private readonly ApnsOptions _options;
    private readonly HttpClient _http;
    private readonly ECDsa _key;
    private readonly ILogger<ApnsPushSender> _logger;
    private readonly object _jwtLock = new();
    private string? _jwt;
    private DateTime _jwtIssuedAt;

    public ApnsPushSender(ApnsOptions options, string privateKeyPem, HttpMessageHandler? handler, ILogger<ApnsPushSender> logger)
    {
        _options = options;
        _logger = logger;
        _key = ECDsa.Create();
        _key.ImportFromPem(privateKeyPem);

        _http = handler is null ? new HttpClient() : new HttpClient(handler);
        _http.BaseAddress = new Uri(options.UseSandbox ? "https://api.sandbox.push.apple.com" : "https://api.push.apple.com");
        _http.Timeout = TimeSpan.FromSeconds(20);
    }

    public DevicePlatform Platform => DevicePlatform.iOS;

    public async Task<IReadOnlyList<string>> SendAsync(IReadOnlyList<string> tokens, PushMessage message, CancellationToken ct)
    {
        var body = BuildPayload(message);
        var dead = new List<string>();
        foreach (var token in tokens)
        {
            if (await SendOneAsync(token, body, message, forceNewJwt: false, ct))
                dead.Add(token);
        }

        return dead;
    }

    /// <summary>Returns true when Apple says the device token is gone for good (app uninstalled).</summary>
    private async Task<bool> SendOneAsync(string token, string body, PushMessage message, bool forceNewJwt, CancellationToken ct)
    {
        // APNs only speaks HTTP/2. The version has to be set on each request: HttpClient.DefaultRequestVersion
        // only applies to requests that HttpClient creates itself (PostAsync and friends).
        using var request = new HttpRequestMessage(HttpMethod.Post, $"/3/device/{token}")
        {
            Version = HttpVersion.Version20,
            VersionPolicy = HttpVersionPolicy.RequestVersionExact,
            Content = new StringContent(body, Encoding.UTF8, "application/json")
        };
        request.Headers.Authorization = new AuthenticationHeaderValue("bearer", Jwt(forceNewJwt));
        request.Headers.TryAddWithoutValidation("apns-topic", _options.BundleId);
        request.Headers.TryAddWithoutValidation("apns-push-type", "alert");
        request.Headers.TryAddWithoutValidation("apns-priority", "10");

        try
        {
            using var response = await _http.SendAsync(request, ct);
            if (response.IsSuccessStatusCode)
                return false;

            var reason = await ReadReasonAsync(response, ct);
            if (response.StatusCode == HttpStatusCode.Gone || reason == "Unregistered")
                return true;

            // The provider token can expire or be refused once; mint a new one and retry a single time.
            if (!forceNewJwt && reason is "ExpiredProviderToken" or "InvalidProviderToken")
                return await SendOneAsync(token, body, message, forceNewJwt: true, ct);

            // BadDeviceToken is also what Apple answers when a sandbox token is used against production (or the reverse),
            // so the token is kept and the problem is logged instead of deleting devices that may be fine.
            _logger.LogWarning("APNs rejected a push for user {UserId}: {Status} {Reason} (environment: {Environment})",
                message.UserId, (int)response.StatusCode, reason, _options.UseSandbox ? "sandbox" : "production");
            return false;
        }
        catch (HttpRequestException ex)
        {
            _logger.LogWarning(ex, "Could not reach APNs for user {UserId}", message.UserId);
            return false;
        }
    }

    private static string BuildPayload(PushMessage message)
    {
        var payload = new Dictionary<string, object?>
        {
            ["aps"] = new Dictionary<string, object?>
            {
                ["alert"] = new Dictionary<string, string> { ["title"] = message.Title, ["body"] = message.Body },
                ["sound"] = "default"
            },
            ["notificationId"] = message.NotificationId.ToString(),
            ["type"] = message.Type
        };
        if (message.OrderId is { } orderId)
            payload["orderId"] = orderId.ToString();
        return JsonSerializer.Serialize(payload);
    }

    private static async Task<string?> ReadReasonAsync(HttpResponseMessage response, CancellationToken ct)
    {
        try
        {
            using var doc = JsonDocument.Parse(await response.Content.ReadAsStringAsync(ct));
            return doc.RootElement.TryGetProperty("reason", out var reason) ? reason.GetString() : null;
        }
        catch (JsonException)
        {
            return null;
        }
    }

    private string Jwt(bool forceNew)
    {
        lock (_jwtLock)
        {
            if (!forceNew && _jwt is not null && DateTime.UtcNow - _jwtIssuedAt < JwtLifetime)
                return _jwt;

            var issuedAt = DateTime.UtcNow;
            var header = Base64Url(JsonSerializer.SerializeToUtf8Bytes(new { alg = "ES256", kid = _options.KeyId }));
            var claims = Base64Url(JsonSerializer.SerializeToUtf8Bytes(new { iss = _options.TeamId, iat = new DateTimeOffset(issuedAt).ToUnixTimeSeconds() }));
            var signed = $"{header}.{claims}";
            // ECDsa.SignData returns the raw R||S form that JWT ES256 requires.
            var signature = _key.SignData(Encoding.UTF8.GetBytes(signed), HashAlgorithmName.SHA256);
            _jwt = $"{signed}.{Base64Url(signature)}";
            _jwtIssuedAt = issuedAt;
            return _jwt;
        }
    }

    private static string Base64Url(byte[] bytes) => Convert.ToBase64String(bytes).TrimEnd('=').Replace('+', '-').Replace('/', '_');

    public void Dispose()
    {
        _http.Dispose();
        _key.Dispose();
    }
}
