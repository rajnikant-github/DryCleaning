using System.Collections.ObjectModel;
using System.Text.RegularExpressions;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using DryCleaning.Helpers;
using DryCleaning.Models;
using DryCleaning.Services;
using DryCleaning.ViewModels.Items;

namespace DryCleaning.ViewModels.Driver;

/// <summary>D14 – Driver profile.</summary>
public sealed partial class DriverProfileViewModel(ViewModelServices services, IDriverService drivers, IAuthService auth)
    : BaseViewModel(services)
{
    private bool _suppressOnline;

    [ObservableProperty] private string name = string.Empty;
    [ObservableProperty] private string initials = string.Empty;
    [ObservableProperty] private string phone = string.Empty;
    [ObservableProperty] private string email = string.Empty;
    [ObservableProperty] private string licenseNumber = string.Empty;
    [ObservableProperty] private string baseArea = string.Empty;
    [ObservableProperty] private string completedJobsText = string.Empty;
    [ObservableProperty] private decimal totalEarnings;
    [ObservableProperty] private string memberSinceText = string.Empty;
    [ObservableProperty] private bool isActive;

    [ObservableProperty] private bool isOnline;

    public string AppVersion => $"Version {AppInfo.Current.VersionString}";

    protected override async Task<bool> LoadDataAsync()
    {
        var profile = await drivers.GetProfileAsync();
        Name = profile.User.Name;
        Initials = StringHelpers.Initials(profile.User.Name);
        Phone = string.IsNullOrWhiteSpace(profile.User.Phone) ? "No phone added" : profile.User.Phone;
        Email = string.IsNullOrWhiteSpace(profile.User.Email) ? "No email added" : profile.User.Email;
        LicenseNumber = profile.Driver.LicenseNumber;
        BaseArea = profile.BaseAddress is null ? "Not set" : $"{profile.BaseAddress.SubLocality}, {profile.BaseAddress.Locality}";
        CompletedJobsText = profile.CompletedJobs == 1 ? "1 job" : $"{profile.CompletedJobs} jobs";
        TotalEarnings = profile.TotalEarnings;
        MemberSinceText = $"Marketplace driver since {profile.User.CreatedAt:MMM yyyy}";
        IsActive = profile.Driver.IsActive;

        _suppressOnline = true;
        IsOnline = profile.Driver.IsAvailable;
        _suppressOnline = false;
        return true;
    }

    partial void OnIsOnlineChanged(bool value)
    {
        if (_suppressOnline)
            return;

        _ = Task.Run(async () =>
        {
            try
            {
                await drivers.SetOnlineAsync(value);
            }
            catch (Exception ex)
            {
                await MainThread.InvokeOnMainThreadAsync(async () =>
                {
                    _suppressOnline = true;
                    IsOnline = !value;
                    _suppressOnline = false;
                    await Dialogs.AlertAsync("Availability", ex.Message);
                });
            }
        });
    }

    [RelayCommand]
    private Task EditProfileAsync() => Navigation.GoToAsync(Routes.DriverEditProfile);

    [RelayCommand]
    private Task OpenEarningsAsync() => Navigation.GoToAsync(Routes.DriverEarnings);

    [RelayCommand]
    private Task OpenNotificationsAsync() => Navigation.GoToAsync(Routes.Notifications);

    [RelayCommand]
    private Task ChangePasswordAsync() => Navigation.GoToAsync(Routes.ChangePassword);

    [RelayCommand]
    private Task HelpAsync() =>
        Dialogs.AlertAsync("Driver support", "Call +27 21 555 0100 (07:00–20:00, every day) or email drivers@edrycleaners.co.za.");

    [RelayCommand]
    private Task SafetyAsync() =>
        Dialogs.AlertAsync("Safety & guidelines",
            "Always verify the pickup code and delivery OTP in person. Never leave an order unattended, and photograph sealed bags at pickup and drop-off.");

    [RelayCommand]
    private async Task LogoutAsync()
    {
        if (!await Dialogs.ConfirmAsync("Log out", "Are you sure you want to log out? You'll stop receiving new jobs.", "Log out", "Cancel"))
            return;

        try
        {
            await drivers.SetOnlineAsync(false);
        }
        catch (Exception)
        {
            // Logging out must always work.
        }

        await auth.LogoutAsync();
        await Navigation.GoToAsync(Routes.RoleSelection);
    }
}
