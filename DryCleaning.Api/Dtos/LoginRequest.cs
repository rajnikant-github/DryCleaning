using DryCleaning.Api.Domain;

namespace DryCleaning.Api.Dtos;

public sealed class LoginRequest
{
    /// <summary>The account's email address (sign-in is by email only).</summary>
    public string Identifier { get; set; } = string.Empty;

    public string Password { get; set; } = string.Empty;
}
