using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using DryCleaning.Helpers;
using DryCleaning.Models;

namespace DryCleaning.ViewModels.Items;

public sealed class NotificationItem
{
    public NotificationItem(Notification notification, Func<NotificationItem, Task> open)
    {
        Notification = notification;
        OpenCommand = new AsyncRelayCommand(() => open(this));
    }

    public Notification Notification { get; }
    public string Icon => CatalogIcons.ForNotification(Notification.Type);
    public string Title => Notification.Title;
    public string Message => Notification.Message;
    public string TimeText => TimeFormat.Relative(Notification.CreatedAt);
    public bool IsUnread => !Notification.IsRead;
    public IAsyncRelayCommand OpenCommand { get; }
}
