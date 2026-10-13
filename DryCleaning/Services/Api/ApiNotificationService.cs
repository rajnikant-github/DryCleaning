using DryCleaning.Models;

namespace DryCleaning.Services.Api;

/// <summary>In-app notifications for the signed-in user (any role).</summary>
public sealed class ApiNotificationService(ApiClient api) : INotificationService
{
    public async Task<IReadOnlyList<Notification>> GetNotificationsAsync()
    {
        var page = await api.GetAsync("api/notifications?pageSize=100", ApiJsonContext.Default.PagedResultNotificationDto);
        return page.Items.Select(ApiMapping.ToNotification).OrderByDescending(n => n.CreatedAt).ToList();
    }

    public Task<int> GetUnreadCountAsync() => api.GetAsync("api/notifications/unread-count", ApiJsonContext.Default.Int32);

    public Task MarkAsReadAsync(Guid notificationId) => api.PostAsync($"api/notifications/{notificationId}/read");

    public Task MarkAllAsReadAsync() => api.PostAsync("api/notifications/read-all");
}
