namespace DryCleaning.Services.Api.Contracts;

public sealed class RegisterConfirmRequest
{
    public string Email { get; set; } = string.Empty;
    public string Code { get; set; } = string.Empty;
}
