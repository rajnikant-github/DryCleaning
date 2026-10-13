using System.Text.RegularExpressions;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using DryCleaning.Helpers;
using DryCleaning.Services;

namespace DryCleaning.ViewModels.Customer;

/// <summary>C19 – Customer profile.</summary>
public sealed partial class CustomerProfileViewModel(
    ViewModelServices services, ICustomerProfileService profiles, IAuthService auth, ICartService cart)
    : BaseViewModel(services)
{
    [ObservableProperty] private string name = string.Empty;
    [ObservableProperty] private string initials = string.Empty;
    [ObservableProperty] private string email = string.Empty;
    [ObservableProperty] private string phone = string.Empty;
    [ObservableProperty] private string addressText = string.Empty;
    [ObservableProperty] private bool hasAddress;
    [ObservableProperty] private string orderCountText = string.Empty;
    [ObservableProperty] private string memberSinceText = string.Empty;

    public string AppVersion => $"Version {AppInfo.Current.VersionString}";

    protected override async Task<bool> LoadDataAsync()
    {
        var profile = await profiles.GetProfileAsync();
        Name = profile.User.Name;
        Initials = StringHelpers.Initials(profile.User.Name);
        Email = string.IsNullOrWhiteSpace(profile.User.Email) ? "No email added" : profile.User.Email;
        Phone = string.IsNullOrWhiteSpace(profile.User.Phone) ? "No phone added" : profile.User.Phone;
        HasAddress = profile.Address is not null;
        AddressText = profile.Address?.FormattedAddress ?? "Add your pickup address";
        OrderCountText = profile.OrderCount == 1 ? "1 order" : $"{profile.OrderCount} orders";
        MemberSinceText = $"Member since {profile.User.CreatedAt:MMM yyyy}";
        return true;
    }

    [RelayCommand]
    private Task EditProfileAsync() => Navigation.GoToAsync(Routes.EditProfile);

    [RelayCommand]
    private Task EditAddressAsync() => Navigation.GoToAsync(Routes.PickupAddress, Param(Routes.ModeKey, "profile"));

    [RelayCommand]
    private Task OpenOrdersAsync() => Navigation.GoToAsync(Routes.CustomerOrders);

    [RelayCommand]
    private Task OpenNotificationsAsync() => Navigation.GoToAsync(Routes.Notifications);

    [RelayCommand]
    private Task HelpAsync() =>
        Dialogs.AlertAsync("Help & Support", "Email help@edrycleaners.co.za or call +27 21 555 0100 (Mon–Sat, 08:00–18:00).");

    [RelayCommand]
    private Task TermsAsync() =>
        Dialogs.AlertAsync("Terms & Privacy", "The full terms of service and privacy policy will be linked here before launch.");

    [RelayCommand]
    private async Task LogoutAsync()
    {
        if (!await Dialogs.ConfirmAsync("Log out", "Are you sure you want to log out?", "Log out", "Cancel"))
            return;

        cart.Clear();
        await auth.LogoutAsync();
        await Navigation.GoToAsync(Routes.RoleSelection);
    }
}

/// <summary>Edit name, email and phone.</summary>
public sealed partial class EditProfileViewModel(ViewModelServices services, ICustomerProfileService profiles)
    : BaseViewModel(services)
{
    private static readonly Regex EmailPattern = new(@"^[^@\s]+@[^@\s]+\.[^@\s]+$", RegexOptions.Compiled);

    [ObservableProperty] private string name = string.Empty;
    [ObservableProperty] private string email = string.Empty;
    [ObservableProperty] private string phone = string.Empty;
    [ObservableProperty] private string validationMessage = string.Empty;
    [ObservableProperty] private bool hasValidationMessage;

    protected override async Task<bool> LoadDataAsync()
    {
        var profile = await profiles.GetProfileAsync();
        Name = profile.User.Name;
        Email = profile.User.Email;
        Phone = profile.User.Phone;
        return true;
    }

    // Only load once – don't overwrite the user's edits when the page re-appears.
    public override Task OnAppearingAsync() => IsContent ? Task.CompletedTask : base.OnAppearingAsync();

    [RelayCommand]
    private Task SaveAsync() => RunBusyAsync(async () =>
    {
        var error = Validate();
        ValidationMessage = error ?? string.Empty;
        HasValidationMessage = error is not null;
        if (error is not null)
            return;

        await profiles.UpdateProfileAsync(Name.Trim(), Email.Trim(), Phone.Trim());
        await Navigation.GoBackAsync();
    });

    private string? Validate()
    {
        if (string.IsNullOrWhiteSpace(Name) || Name.Trim().Length < 2)
            return "Please enter your full name.";
        if (string.IsNullOrWhiteSpace(Email) && string.IsNullOrWhiteSpace(Phone))
            return "Add an email address or a phone number so we can reach you.";
        if (!string.IsNullOrWhiteSpace(Email) && !EmailPattern.IsMatch(Email.Trim()))
            return "Please enter a valid email address.";
        if (!string.IsNullOrWhiteSpace(Phone) && Phone.Count(char.IsDigit) < 9)
            return "Please enter a valid phone number.";
        return null;
    }
}
