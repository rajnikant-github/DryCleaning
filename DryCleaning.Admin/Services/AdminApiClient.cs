using System.Net;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Components.Authorization;

namespace DryCleaning.Admin.Services;

/// <summary>Calls the E Dry Cleaners API as the signed-in admin (the access token lives in the sign-in cookie's claims).</summary>
public sealed class AdminApiClient(IHttpClientFactory factory, AuthenticationStateProvider auth)
{
    public const string HttpClientName = "Api";
    public const string TokenClaim = "access_token";

    public static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web)
    {
        Converters = { new JsonStringEnumConverter() }
    };

    private readonly HttpClient _http = factory.CreateClient(HttpClientName);

    public Task<T> GetAsync<T>(string path) => SendAsync<T>(HttpMethod.Get, path, null);

    public Task<T> PostAsync<T>(string path, object? body = null) => SendAsync<T>(HttpMethod.Post, path, body);

    public Task<T> PutAsync<T>(string path, object body) => SendAsync<T>(HttpMethod.Put, path, body);

    public Task PutAsync(string path, object body) => SendAsync(HttpMethod.Put, path, body);

    public Task PostAsync(string path, object body) => SendAsync(HttpMethod.Post, path, body);

    public Task DeleteAsync(string path) => SendAsync(HttpMethod.Delete, path, null);

    public async Task<T> SendAsync<T>(HttpMethod method, string path, object? body)
    {
        using var response = await SendRawAsync(method, path, body);
        var value = await response.Content.ReadFromJsonAsync<T>(Json);
        return value ?? throw new ApiException(response.StatusCode, "The server sent an empty answer.");
    }

    public async Task SendAsync(HttpMethod method, string path, object? body)
    {
        using var response = await SendRawAsync(method, path, body);
    }

    private async Task<HttpResponseMessage> SendRawAsync(HttpMethod method, string path, object? body)
    {
        var state = await auth.GetAuthenticationStateAsync();
        var token = state.User.FindFirst(TokenClaim)?.Value;

        using var request = new HttpRequestMessage(method, path);
        if (!string.IsNullOrEmpty(token))
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        if (body is not null)
            request.Content = new StringContent(JsonSerializer.Serialize(body, Json), Encoding.UTF8, "application/json");

        HttpResponseMessage response;
        try
        {
            response = await _http.SendAsync(request);
        }
        catch (HttpRequestException ex)
        {
            throw new ApiException(HttpStatusCode.ServiceUnavailable, $"Can't reach the API ({ex.Message}). Is it running?");
        }

        if (response.IsSuccessStatusCode)
            return response;

        var message = await ReadProblemAsync(response);
        response.Dispose();
        throw new ApiException(response.StatusCode, message);
    }

    /// <summary>Turns an RFC 7807 problem answer into one readable line.</summary>
    public static async Task<string> ReadProblemAsync(HttpResponseMessage response)
    {
        if (response.StatusCode == HttpStatusCode.Unauthorized)
            return "Your session has expired. Please sign in again.";
        if (response.StatusCode == HttpStatusCode.Forbidden)
            return "You don't have permission to do that.";

        try
        {
            using var doc = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
            var root = doc.RootElement;
            if (root.TryGetProperty("errors", out var errors) && errors.ValueKind == JsonValueKind.Object)
            {
                var lines = errors.EnumerateObject()
                    .SelectMany(e => e.Value.ValueKind == JsonValueKind.Array ? e.Value.EnumerateArray().Select(v => v.GetString()) : [e.Value.GetString()])
                    .Where(s => !string.IsNullOrWhiteSpace(s));
                var joined = string.Join(" ", lines);
                if (joined.Length > 0)
                    return joined;
            }

            if (root.TryGetProperty("detail", out var detail) && detail.GetString() is { Length: > 0 } d)
                return d;
            if (root.TryGetProperty("title", out var title) && title.GetString() is { Length: > 0 } t)
                return t;
        }
        catch (JsonException)
        {
            // Not a problem document – fall through to the generic message.
        }

        return $"The API answered {(int)response.StatusCode} {response.ReasonPhrase}.";
    }
}
