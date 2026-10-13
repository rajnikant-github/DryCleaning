using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using DryCleaning.Api.Common;
using Microsoft.Extensions.Options;

namespace DryCleaning.Api.Infrastructure.Payments;

public sealed class PaystackInitializeResult
{
    public required string AuthorizationUrl { get; init; }
    public required string AccessCode { get; init; }
    public required string Reference { get; init; }
}
