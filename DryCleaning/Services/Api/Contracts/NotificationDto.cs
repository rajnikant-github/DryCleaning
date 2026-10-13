using DryCleaning.Models;

namespace DryCleaning.Services.Api.Contracts;

/// <summary>In-app notification.</summary>
public sealed class NotificationDto
{
    public Guid Id { get; set; }
    public Guid? OrderId { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
    public NotificationType Type { get; set; }
    public bool IsRead { get; set; }
    public DateTime CreatedAt { get; set; }
}
