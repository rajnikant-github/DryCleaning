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

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(HasLogo))]
    private ImageSource? logoSource;

    public bool HasLogo => LogoSource is not null;

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
        LogoSource = ImageSources.FromPathOrUrl(profile.Provider.LogoUrl);
        CategoriesText = string.Join("  ·  ", profile.Categories.Select(c => $"{CatalogIcons.ForCategory(c.Name)} {c.Name}"));
        return true;
    }

    [RelayCommand]
    private Task OpenNotificationsAsync() => Navigation.GoToAsync(Routes.Notifications);

    [RelayCommand]
    private Task EditBusinessProfileAsync() => Navigation.GoToAsync(Routes.EditBusinessProfile);

    [RelayCommand]
    private Task ManageCategoriesAsync() => Navigation.GoToAsync(Routes.ProviderCategories);

    [RelayCommand]
    private Task ChangePasswordAsync() => Navigation.GoToAsync(Routes.ChangePassword);

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
