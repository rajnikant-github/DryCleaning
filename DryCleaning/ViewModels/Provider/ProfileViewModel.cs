using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using DryCleaning.Helpers;
using DryCleaning.Models;
using DryCleaning.Services;
using DryCleaning.Services.Realtime;
using DryCleaning.ViewModels.Items;

namespace DryCleaning.ViewModels.Provider;

public sealed partial class ProfileViewModel(ViewModelServices services, IProviderProfileService profiles, IAuthService auth, ProfilePhotoChanger photoChanger)
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
    [ObservableProperty] private ImageSource? photoSource;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(HasLogo))]
    private ImageSource? logoSource;

    /// <summary>Under review / deactivated notice (hidden when the business is approved).</summary>
    [ObservableProperty] private ProfileStatus accountStatus;

    public bool HasLogo => LogoSource is not null;

    // Approval (or deactivation) arrives as a notification: look again straight away.
    protected override bool ReactsTo(RealtimeChangedMessage message) =>
        message.Kind == RealtimeChangeKind.NotificationReceived || message.Kind == RealtimeChangeKind.Reconnected;

    protected override async Task<bool> LoadDataAsync()
    {
        AccountStatus = (await auth.RefreshAccountAsync())?.ProfileStatus ?? AccountStatus;
        var profile = await profiles.GetProfileAsync();
        BusinessName = profile.Provider.BusinessName;
        Description = profile.Provider.Description;
        OwnerName = profile.Owner.Name;
        OwnerInitials = StringHelpers.Initials(profile.Owner.Name);
        PhotoSource = ImageSources.FromPathOrUrl(profile.Owner.PhotoUrl);
        Phone = profile.Provider.Phone;
        Email = profile.Provider.Email;
        Address = profile.Address.FormattedAddress;
        LogoSource = ImageSources.FromPathOrUrl(profile.Provider.LogoUrl);
        CategoriesText = string.Join("  ·  ", profile.Categories.Select(c => c.Name));
        return true;
    }

    [RelayCommand]
    private Task ChangePhotoAsync() => RunBusyAsync(async () =>
    {
        var result = await photoChanger.RunAsync(PhotoSource is not null);
        if (result.Changed)
            PhotoSource = ImageSources.FromPathOrUrl(result.Url);
    });

    [RelayCommand]
    private Task OpenNotificationsAsync() => Navigation.GoToAsync(Routes.Notifications);

    [RelayCommand]
    private Task EditBusinessProfileAsync() => Navigation.GoToAsync(Routes.EditBusinessProfile);

    [RelayCommand]
    private Task ManageCategoriesAsync() => Navigation.GoToAsync(Routes.ProviderCategories);

    [RelayCommand]
    private Task ManageWorkingHoursAsync() => Navigation.GoToAsync(Routes.WorkingHours);

    [RelayCommand]
    private Task ChangePasswordAsync() => Navigation.GoToAsync(Routes.ChangePassword);

    [RelayCommand]
    private Task OpenServicesAsync() => Navigation.GoToAsync("//provider/services");

    [RelayCommand]
    private Task EditAddressAsync() =>
        Navigation.GoToAsync(Routes.PickupAddress, Param(Routes.ModeKey, Customer.PickupAddressViewModel.ProviderMode));

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
