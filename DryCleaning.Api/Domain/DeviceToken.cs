namespace DryCleaning.Api.Domain;

/// <summary>A phone registered for push notifications (Firebase Cloud Messaging token).</summary>
public class DeviceToken : EntityBase
{
    public Guid UserId { get; set; }
    public DevicePlatform Platform { get; set; }

    /// <summary>FCM registration token – unique across all users.</summary>
    public string Token { get; set; } = string.Empty;
    public DateTime LastSeenAt { get; set; } = DateTime.UtcNow;

    public User User { get; set; } = null!;
}
