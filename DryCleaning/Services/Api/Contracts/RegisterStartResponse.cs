namespace DryCleaning.Services.Api.Contracts;

public sealed class RegisterStartResponse
{
    public string Destination { get; set; } = string.Empty;
    public int ExpiresInSeconds { get; set; }
    public int ResendInSeconds { get; set; } = 60;

}
