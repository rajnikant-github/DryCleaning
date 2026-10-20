using DryCleaning.Services.Api.Contracts;
using DryCleaning.Services.Push;

namespace DryCleaning.Services.Api;

public sealed class ApiPushRegistry(ApiClient api) : IPushRegistry
{
    public Task RegisterAsync(string token, string platform) =>
        api.SendAsync(HttpMethod.Post, "api/me/devices", new RegisterDeviceRequest { Token = token, Platform = platform },
            ApiJsonContext.Default.RegisterDeviceRequest);

    public Task UnregisterAsync(string token) => api.DeleteAsync($"api/me/devices?token={Uri.EscapeDataString(token)}");
}
