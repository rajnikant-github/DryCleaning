using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using DryCleaning.Api.Common;
using Microsoft.Extensions.Options;

namespace DryCleaning.Api.Infrastructure.Payments;

public interface IPaystackClient
{
    Task<PaystackInitializeResult> InitializeAsync(string email, decimal amount, string currency, string reference, IDictionary<string, string> metadata, CancellationToken ct);
    Task<PaystackVerifyResult> VerifyAsync(string reference, CancellationToken ct);
    bool IsValidSignature(string rawBody, string? signature);
}
