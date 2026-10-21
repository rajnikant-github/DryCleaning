namespace DryCleaning.Api.Dtos;

public sealed class SendNotificationRequest
{
    public NotificationAudience Audience { get; set; }

    /// <summary>Required when <see cref="Audience"/> is <c>User</c>.</summary>
    public Guid? UserId { get; set; }

    public string Title { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;

    /// <summary>Also email the announcement to recipients who have an email address.</summary>
    public bool AlsoEmail { get; set; }
}
