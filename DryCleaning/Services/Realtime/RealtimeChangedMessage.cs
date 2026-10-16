namespace DryCleaning.Services.Realtime;

/// <summary>Sent through the messenger when the API reports a change that concerns the signed-in user.</summary>
public sealed class RealtimeChangedMessage
{
    public required RealtimeChangeKind Kind { get; init; }
    public Guid? OrderId { get; init; }
    public Guid? JobId { get; init; }

    /// <summary>Order or job status as sent by the API (e.g. "Confirmed", "Assigned").</summary>
    public string? Status { get; init; }
}
