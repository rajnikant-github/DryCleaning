using DryCleaning.Api.Domain;

namespace DryCleaning.Api.Dtos;

public sealed class ResetPasswordRequest
{
    public string Identifier { get; set; } = string.Empty;
    public string Code { get; set; } = string.Empty;
    public string NewPassword { get; set; } = string.Empty;
}
