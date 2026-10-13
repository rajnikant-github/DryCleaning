using DryCleaning.Api.Domain;

namespace DryCleaning.Api.Dtos;

public sealed class AuthResponse
{
    public required string AccessToken { get; init; }

    /// <summary>When the access token expires – call POST /api/auth/refresh before then.</summary>
    public required DateTime ExpiresAt { get; init; }

    public required UserDto User { get; init; }

    /// <summary>Keep in secure storage; exchange it at POST /api/auth/refresh. Rotates on every use.</summary>
    public string? RefreshToken { get; set; }

    public DateTime? RefreshTokenExpiresAt { get; set; }
}
