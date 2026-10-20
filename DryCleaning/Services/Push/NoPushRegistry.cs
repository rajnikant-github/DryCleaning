namespace DryCleaning.Services.Push;

/// <summary>Demo (mock) mode has no server to register with.</summary>
public sealed class NoPushRegistry : IPushRegistry
{
    public Task RegisterAsync(string token, string platform) => Task.CompletedTask;

    public Task UnregisterAsync(string token) => Task.CompletedTask;
}
