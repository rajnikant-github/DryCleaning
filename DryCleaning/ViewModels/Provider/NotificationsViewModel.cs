using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using DryCleaning.Helpers;
using DryCleaning.Services;
using DryCleaning.Services.Realtime;
using DryCleaning.ViewModels.Items;

namespace DryCleaning.ViewModels.Provider;

public sealed partial class NotificationsViewModel(ViewModelServices services, INotificationService notifications, IOrderLinkService orderLinks)
    : BaseViewModel(services)
{
    protected override bool ReactsTo(RealtimeChangedMessage message) =>
        message.Kind == RealtimeChangeKind.Reconnected || message.Kind == RealtimeChangeKind.NotificationReceived;

    public ObservableCollection<NotificationItem> Items { get; } = [];

    protected override async Task<bool> LoadDataAsync()
    {
        var list = await notifications.GetNotificationsAsync();
        Items.Clear();
        foreach (var notification in list)
            Items.Add(new NotificationItem(notification, OpenAsync));
        return Items.Count > 0;
    }

    private async Task OpenAsync(NotificationItem item)
    {
        await notifications.MarkAsReadAsync(item.Notification.Id);
        if (item.Notification.OrderId is not { } orderId)
        {
            await LoadAsync(showSkeleton: false);
            return;
        }

        await orderLinks.OpenAsync(orderId);
    }

    [RelayCommand]
    private async Task MarkAllReadAsync()
    {
        await notifications.MarkAllAsReadAsync();
        await LoadAsync(showSkeleton: false);
    }
}
