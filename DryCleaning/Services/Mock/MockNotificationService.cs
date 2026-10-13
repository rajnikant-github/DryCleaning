using DryCleaning.Models;

namespace DryCleaning.Services.Mock;

public sealed class MockNotificationService(MockDataStore store, ISessionService session) : INotificationService
{
    private Guid UserId => session.CurrentUser?.Id ?? store.DemoProvider.UserId;

    public async Task<IReadOnlyList<Notification>> GetNotificationsAsync()
    {
        await MockDataStore.SimulateLatencyAsync();
        lock (store.Gate)
            return store.Notifications.Where(n => n.UserId == UserId).OrderByDescending(n => n.CreatedAt).ToList();
    }

    public Task<int> GetUnreadCountAsync()
    {
        lock (store.Gate)
            return Task.FromResult(store.Notifications.Count(n => n.UserId == UserId && !n.IsRead));
    }

    public Task MarkAsReadAsync(Guid notificationId)
    {
        lock (store.Gate)
        {
            var notification = store.Notifications.FirstOrDefault(n => n.Id == notificationId);
            if (notification is not null)
                notification.IsRead = true;
        }

        return Task.CompletedTask;
    }

    public Task MarkAllAsReadAsync()
    {
        lock (store.Gate)
        {
            foreach (var notification in store.Notifications.Where(n => n.UserId == UserId))
                notification.IsRead = true;
        }

        return Task.CompletedTask;
    }
}
