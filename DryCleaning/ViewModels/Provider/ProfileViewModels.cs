using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using DryCleaning.Helpers;
using DryCleaning.Services;
using DryCleaning.ViewModels.Items;

namespace DryCleaning.ViewModels.Provider;

public sealed partial class ProfileViewModel(ViewModelServices services, IProviderProfileService profiles, IAuthService auth)
    : BaseViewModel(services)
{
    [ObservableProperty] private string businessName = string.Empty;
    [ObservableProperty] private string description = string.Empty;
    [ObservableProperty] private string ownerName = string.Empty;
    [ObservableProperty] private string phone = string.Empty;
    [ObservableProperty] private string email = string.Empty;
    [ObservableProperty] private string address = string.Empty;
    [ObservableProperty] private string categoriesText = string.Empty;
    [ObservableProperty] private string ownerInitials = string.Empty;

    protected override async Task<bool> LoadDataAsync()
    {
        var profile = await profiles.GetProfileAsync();
        BusinessName = profile.Provider.BusinessName;
        Description = profile.Provider.Description;
        OwnerName = profile.Owner.Name;
        OwnerInitials = StringHelpers.Initials(profile.Owner.Name);
        Phone = profile.Provider.Phone;
        Email = profile.Provider.Email;
        Address = profile.Address.FormattedAddress;
        CategoriesText = string.Join("  ·  ", profile.Categories.Select(c => $"{CatalogIcons.ForCategory(c.Name)} {c.Name}"));
        return true;
    }

    [RelayCommand]
    private Task OpenNotificationsAsync() => Navigation.GoToAsync(Routes.Notifications);

    [RelayCommand]
    private Task OpenServicesAsync() => Navigation.GoToAsync("//provider/services");

    [RelayCommand]
    private Task EditAddressAsync() =>
        Dialogs.AlertAsync("Business address", "Address search with Google Places will be enabled when the live API is connected.");

    [RelayCommand]
    private Task HelpAsync() =>
        Dialogs.AlertAsync("Help & Support", "Email partners@edrycleaners.co.za or call +27 21 555 0100 (Mon–Fri, 08:00–17:00).");

    [RelayCommand]
    private async Task LogoutAsync()
    {
        if (!await Dialogs.ConfirmAsync("Log out", "Are you sure you want to log out?", "Log out", "Cancel"))
            return;

        await auth.LogoutAsync();
        await Navigation.GoToAsync(Routes.RoleSelection);
    }
}

public sealed partial class NotificationsViewModel(ViewModelServices services, INotificationService notifications, ISessionService session)
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
        if (item.Notification.OrderId is { } orderId)
            await Navigation.GoToAsync(
                session.CurrentUser?.Role == Models.UserRole.Customer ? Routes.CustomerOrderDetail : Routes.OrderDetail,
                Param(Routes.OrderIdKey, orderId));
        else
            await LoadAsync(showSkeleton: false);
    }

    [RelayCommand]
    private async Task MarkAllReadAsync()
    {
        await notifications.MarkAllAsReadAsync();
        await LoadAsync(showSkeleton: false);
    }
}
