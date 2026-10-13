using DryCleaning.Api.Domain;

namespace DryCleaning.Api.Dtos;

public sealed class RequestOtpResponse
{
    public required string Destination { get; init; }
    public required int ExpiresInSeconds { get; init; }

    /// <summary>Only returned when Otp:ExposeCodeInResponse is true (development).</summary>
    public required string? DevCode { get; init; }
}
