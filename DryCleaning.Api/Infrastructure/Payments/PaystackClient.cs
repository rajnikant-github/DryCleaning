using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using DryCleaning.Api.Common;
using Microsoft.Extensions.Options;

namespace DryCleaning.Api.Infrastructure.Payments;

public sealed record PaystackInitializeResult(string AuthorizationUrl, string AccessCode, string Reference);

/// <param name="Status">Paystack status: success, failed, abandoned, ongoing, pending, reversed…</param>
/// <param name="AmountSubunit">Amount in cents.</param>
public sealed record PaystackVerifyResult(string Status, long AmountSubunit, string Currency, DateTime? PaidAt, string? Channel, string Reference);

public interface IPaystackClient
{
    Task<PaystackInitializeResult> InitializeAsync(string email, decimal amount, string currency, string reference, IDictionary<string, string> metadata, CancellationToken ct);
    Task<PaystackVerifyResult> VerifyAsync(string reference, CancellationToken ct);
    bool IsValidSignature(string rawBody, string? signature);
}

/// <summary>
/// Paystack REST client (https://paystack.com/docs/api/transaction/).
/// Amounts are sent in the currency subunit (ZAR cents).
/// </summary>
public sealed class PaystackClient(HttpClient http, IOptions<PaystackOptions> options, ILogger<PaystackClient> logger) : IPaystackClient
{
    private static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web);

    public async Task<PaystackInitializeResult> InitializeAsync(
        string email, decimal amount, string currency, string reference, IDictionary<string, string> metadata, CancellationToken ct)
    {
        var body = new Dictionary<string, object?>
        {
            ["email"] = email,
            ["amount"] = ToSubunit(amount),
            ["currency"] = currency,
            ["reference"] = reference,
            ["metadata"] = metadata
        };
        if (!string.IsNullOrWhiteSpace(options.Value.CallbackUrl))
            body["callback_url"] = options.Value.CallbackUrl;

        using var request = new HttpRequestMessage(HttpMethod.Post, "transaction/initialize") { Content = JsonContent.Create(body) };
        Authorize(request);

        var response = await SendAsync<InitializeData>(request, ct);
        return new PaystackInitializeResult(response.AuthorizationUrl, response.AccessCode, response.Reference);
    }

    public async Task<PaystackVerifyResult> VerifyAsync(string reference, CancellationToken ct)
    {
        using var request = new HttpRequestMessage(HttpMethod.Get, $"transaction/verify/{Uri.EscapeDataString(reference)}");
        Authorize(request);

        var data = await SendAsync<VerifyData>(request, ct);
        return new PaystackVerifyResult(data.Status ?? "unknown", data.Amount, data.Currency ?? string.Empty, data.PaidAt, data.Channel, data.Reference ?? reference);
    }

    /// <summary>Paystack signs webhooks with HMAC-SHA512 of the raw body using the secret key.</summary>
    public bool IsValidSignature(string rawBody, string? signature)
    {
        if (string.IsNullOrWhiteSpace(signature) || string.IsNullOrEmpty(options.Value.SecretKey))
            return false;

        var hash = HMACSHA512.HashData(Encoding.UTF8.GetBytes(options.Value.SecretKey), Encoding.UTF8.GetBytes(rawBody));
        var expected = Convert.ToHexStringLower(hash);
        return CryptographicOperations.FixedTimeEquals(Encoding.ASCII.GetBytes(expected), Encoding.ASCII.GetBytes(signature.Trim().ToLowerInvariant()));
    }

    public static long ToSubunit(decimal amount) => (long)Math.Round(amount * 100m, MidpointRounding.AwayFromZero);

    private void Authorize(HttpRequestMessage request)
    {
        if (string.IsNullOrWhiteSpace(options.Value.SecretKey))
            throw new BadRequestException("Paystack is not configured. Set Paystack:SecretKey.");
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", options.Value.SecretKey);
    }

    private async Task<T> SendAsync<T>(HttpRequestMessage request, CancellationToken ct)
    {
        using var response = await http.SendAsync(request, ct);
        var envelope = await response.Content.ReadFromJsonAsync<Envelope<T>>(Json, ct);

        if (!response.IsSuccessStatusCode || envelope is null || !envelope.Status || envelope.Data is null)
        {
            logger.LogWarning("Paystack {Path} failed: {Status} {Message}", request.RequestUri, (int)response.StatusCode, envelope?.Message);
            throw new BadRequestException($"Payment provider error: {envelope?.Message ?? response.ReasonPhrase}");
        }

        return envelope.Data;
    }

    private sealed class Envelope<T>
    {
        public bool Status { get; set; }
        public string? Message { get; set; }
        public T? Data { get; set; }
    }

    private sealed class InitializeData
    {
        [JsonPropertyName("authorization_url")] public string AuthorizationUrl { get; set; } = string.Empty;
        [JsonPropertyName("access_code")] public string AccessCode { get; set; } = string.Empty;
        [JsonPropertyName("reference")] public string Reference { get; set; } = string.Empty;
    }

    private sealed class VerifyData
    {
        [JsonPropertyName("status")] public string? Status { get; set; }
        [JsonPropertyName("amount")] public long Amount { get; set; }
        [JsonPropertyName("currency")] public string? Currency { get; set; }
        [JsonPropertyName("paid_at")] public DateTime? PaidAt { get; set; }
        [JsonPropertyName("channel")] public string? Channel { get; set; }
        [JsonPropertyName("reference")] public string? Reference { get; set; }
    }
}
