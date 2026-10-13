using DryCleaning.Models;

namespace DryCleaning.Services;

public interface INotificationService
{
    Task<IReadOnlyList<Notification>> GetNotificationsAsync();
    Task<int> GetUnreadCountAsync();
    Task MarkAsReadAsync(Guid notificationId);
    Task MarkAllAsReadAsync();
}
