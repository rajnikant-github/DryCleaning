using Microsoft.AspNetCore.SignalR.Client;

namespace DryCleaning.Services.Realtime;

/// <summary>Reconnect quickly at first, then every 30 seconds for as long as the user stays signed in.</summary>
public sealed class RealtimeRetryPolicy : IRetryPolicy
{
    private static readonly TimeSpan[] Delays =
        [TimeSpan.Zero, TimeSpan.FromSeconds(2), TimeSpan.FromSeconds(5), TimeSpan.FromSeconds(10), TimeSpan.FromSeconds(30)];

    public static TimeSpan DelayFor(int attempt) => Delays[Math.Min(attempt, Delays.Length - 1)];

    public TimeSpan? NextRetryDelay(RetryContext retryContext) => DelayFor((int)Math.Min(retryContext.PreviousRetryCount, int.MaxValue));
}
