namespace DryCleaning.Api.Infrastructure.Realtime;

/// <summary>Everything one successful SaveChanges changed that apps should hear about.</summary>
public sealed class RealtimeBatch
{
    public HashSet<Guid> OrderIds { get; } = [];
    public List<CapturedJobChange> Jobs { get; } = [];
    public List<NotificationReceivedEvent> Notifications { get; } = [];

    public bool IsEmpty => OrderIds.Count == 0 && Jobs.Count == 0 && Notifications.Count == 0;
}
