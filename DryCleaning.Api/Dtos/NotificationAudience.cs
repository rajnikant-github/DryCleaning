namespace DryCleaning.Api.Dtos;

/// <summary>Who an admin announcement goes to. "All" groups only include active customers, providers and drivers.</summary>
public enum NotificationAudience
{
    /// <summary>One user (UserId).</summary>
    User,
    AllUsers,
    Customers,
    Providers,
    Drivers
}
