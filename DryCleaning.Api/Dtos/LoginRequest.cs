using DryCleaning.Api.Domain;

namespace DryCleaning.Api.Dtos;

public sealed class LoginRequest
{
    /// <summary>Mobile number or email.</summary>
    public string Identifier { get; set; } = string.Empty;

    public string Password { get; set; } = string.Empty;
}
