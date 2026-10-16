namespace DryCleaning.Services.Realtime;

public enum RealtimeChangeKind
{
    OrderUpdated,
    JobChanged,
    NotificationReceived,

    /// <summary>The connection came back after an outage – events in between were missed, so refresh.</summary>
    Reconnected
}
