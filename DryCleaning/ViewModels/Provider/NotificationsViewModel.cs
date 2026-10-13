using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using DryCleaning.Helpers;
using DryCleaning.Services;
using DryCleaning.ViewModels.Items;

namespace DryCleaning.ViewModels.Provider;

public sealed partial class NotificationsViewModel(ViewModelServices services, INotificationService notifications, ISessionService session, IDriverService drivers)
    : BaseViewModel(services)
{
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

        switch (session.CurrentUser?.Role)
        {
            case Models.UserRole.Customer:
                await Navigation.GoToAsync(Routes.CustomerOrderDetail, Param(Routes.OrderIdKey, orderId));
                break;
            case Models.UserRole.Driver:
                if (await drivers.FindJobForOrderAsync(orderId) is { } jobId)
                    await Navigation.GoToAsync(Routes.DriverJobDetail, Param(Routes.JobIdKey, jobId));
                else
                    await Dialogs.AlertAsync("Job unavailable", "This job is no longer available.");
                break;
            default:
                await Navigation.GoToAsync(Routes.OrderDetail, Param(Routes.OrderIdKey, orderId));
                break;
        }
    }

    [RelayCommand]
    private async Task MarkAllReadAsync()
    {
        await notifications.MarkAllAsReadAsync();
        await LoadAsync(showSkeleton: false);
    }
}
