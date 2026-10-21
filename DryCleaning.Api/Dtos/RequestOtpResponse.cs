using DryCleaning.Api.Domain;

namespace DryCleaning.Api.Dtos;

public sealed class RequestOtpResponse
{
    public required string Destination { get; init; }
    public required int ExpiresInSeconds { get; init; }

}
