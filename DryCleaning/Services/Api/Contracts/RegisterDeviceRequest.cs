namespace DryCleaning.Services.Api.Contracts;

/// <summary>POST api/me/devices.</summary>
public sealed class RegisterDeviceRequest
{
    public string Token { get; set; } = string.Empty;

    /// <summary>"Android" or "iOS".</summary>
    public string Platform { get; set; } = string.Empty;
}
