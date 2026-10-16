namespace DryCleaning.Services;

/// <summary>
/// Live updates from the API (orders, driver jobs, notifications). Changes are published as
/// <see cref="Realtime.RealtimeChangedMessage"/>; screens that opt in reload quietly.
/// </summary>
public interface IRealtimeService
{
    bool IsConnected { get; }

    /// <summary>Connects (and keeps reconnecting) while the user is signed in. Safe to call repeatedly.</summary>
    Task StartAsync();

    /// <summary>Closes the connection – on sign-out and when the app goes to the background.</summary>
    Task StopAsync();
}
