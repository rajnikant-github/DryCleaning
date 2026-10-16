namespace DryCleaning.Api.Infrastructure.Realtime;

/// <summary>Client method names the hub calls.</summary>
public static class RealtimeEvents
{
    /// <summary><see cref="OrderUpdatedEvent"/> – to the order's customer, provider and driver(s).</summary>
    public const string OrderUpdated = "OrderUpdated";

    /// <summary><see cref="DriverJobChangedEvent"/> – to online drivers and the job's (previous) driver.</summary>
    public const string DriverJobChanged = "DriverJobChanged";

    /// <summary><see cref="NotificationReceivedEvent"/> – to the notification's user.</summary>
    public const string NotificationReceived = "NotificationReceived";
}
