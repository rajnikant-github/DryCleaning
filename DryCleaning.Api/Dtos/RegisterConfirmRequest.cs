namespace DryCleaning.Api.Dtos;

public sealed class RegisterConfirmRequest
{
    public string Email { get; set; } = string.Empty;
    public string Code { get; set; } = string.Empty;
}
