using DryCleaning.Api.Domain;

namespace DryCleaning.Api.Dtos;

// Notifications

public sealed class NotificationDto
{
    public required Guid Id { get; init; }
    public required Guid? OrderId { get; init; }
    public required string Title { get; init; }
    public required string Message { get; init; }
    public required NotificationType Type { get; init; }
    public required bool IsRead { get; init; }
    public required DateTime CreatedAt { get; init; }
}
