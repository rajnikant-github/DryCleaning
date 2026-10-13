using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using DryCleaning.Api.Common;
using Microsoft.Extensions.Options;

namespace DryCleaning.Api.Infrastructure.Payments;

public sealed class PaystackVerifyResult
{
    /// <summary>Paystack status: success, failed, abandoned, ongoing, pending, reversed…</summary>
    public required string Status { get; init; }

    /// <summary>Amount in cents.</summary>
    public required long AmountSubunit { get; init; }

    public required string Currency { get; init; }
    public required DateTime? PaidAt { get; init; }
    public required string? Channel { get; init; }
    public required string Reference { get; init; }
}
