using DryCleaning.Api.Domain;

namespace DryCleaning.Api.Dtos;

public sealed class RegisterDeviceRequest
{
    public string Token { get; set; } = string.Empty;
    public DevicePlatform Platform { get; set; }
}
