namespace DryCleaning.Services.Push;

/// <summary>Demo (mock) mode: no server, so no push.</summary>
public sealed class NoPushService : IPushService
{
    public Task EnableAsync() => Task.CompletedTask;

    public Task DisableAsync() => Task.CompletedTask;
}
