namespace DryCleaning.Admin.Models;

public sealed class SendNotificationRequest
{
    public NotificationAudience Audience { get; set; }
    public Guid? UserId { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
}
