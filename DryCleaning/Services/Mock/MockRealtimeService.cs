namespace DryCleaning.Services.Mock;

/// <summary>Demo mode has no server to listen to.</summary>
public sealed class MockRealtimeService : IRealtimeService
{
    public bool IsConnected => false;
    public Task StartAsync() => Task.CompletedTask;
    public Task StopAsync() => Task.CompletedTask;
}
