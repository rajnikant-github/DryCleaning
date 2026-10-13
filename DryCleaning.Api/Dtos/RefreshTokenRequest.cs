using DryCleaning.Api.Domain;

namespace DryCleaning.Api.Dtos;

public sealed class RefreshTokenRequest
{
    public string RefreshToken { get; set; } = string.Empty;
}
