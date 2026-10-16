using DryCleaning.Api.Domain;

namespace DryCleaning.Api.Infrastructure.Realtime;

/// <summary>A new row in the user's notification list (bell + badge).</summary>
public sealed class NotificationReceivedEvent
{
    public required Guid NotificationId { get; init; }
    public required Guid UserId { get; init; }
    public required string Title { get; init; }
    public required string Message { get; init; }
    public required NotificationType Type { get; init; }
    public required Guid? OrderId { get; init; }
    public required DateTime CreatedAt { get; init; }
}
