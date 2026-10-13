using System.Collections.ObjectModel;
using System.Text.RegularExpressions;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using DryCleaning.Helpers;
using DryCleaning.Models;
using DryCleaning.Services;
using DryCleaning.ViewModels.Items;

namespace DryCleaning.ViewModels.Driver;

/// <summary>D13 – Earnings for the last 7 days or 4 weeks.</summary>
public sealed partial class DriverEarningsViewModel(ViewModelServices services, IDriverService drivers)
    : BaseViewModel(services)
{
    private EarningsPeriod _period = EarningsPeriod.Week;

    public ObservableCollection<SegmentItem> Periods { get; } = [];
    public ObservableCollection<EarningsBarItem> Bars { get; } = [];
    public ObservableCollection<DriverJobCardItem> Jobs { get; } = [];

    [ObservableProperty] private decimal total;
    [ObservableProperty] private string periodText = string.Empty;
    [ObservableProperty] private string jobCountText = string.Empty;
    [ObservableProperty] private decimal averagePerJob;
    [ObservableProperty] private string splitText = string.Empty;
    [ObservableProperty] private string rateText = string.Empty;
    [ObservableProperty] private bool hasJobs;

    protected override async Task<bool> LoadDataAsync()
    {
        if (Periods.Count == 0)
        {
            Periods.Add(new SegmentItem(nameof(EarningsPeriod.Week), "This Week", OnPeriodSelected) { IsSelected = true });
            Periods.Add(new SegmentItem(nameof(EarningsPeriod.Month), "Last 4 Weeks", OnPeriodSelected));
        }

        var earnings = await drivers.GetEarningsAsync(_period);
        Total = earnings.Total;
        PeriodText = earnings.PeriodText;
        JobCountText = earnings.JobCount == 1 ? "1 job" : $"{earnings.JobCount} jobs";
        AveragePerJob = earnings.AveragePerJob;
        SplitText = $"📦 {earnings.PickupCount} pickups   🚚 {earnings.DeliveryCount} deliveries";
        RateText = $"You earn {CurrencyFormatter.Current.Format(earnings.RatePerJob)} per completed pickup or delivery.";

        var max = earnings.Bars.Count == 0 ? 0 : earnings.Bars.Max(b => b.Amount);
        Bars.Clear();
        foreach (var bar in earnings.Bars)
            Bars.Add(new EarningsBarItem(bar, max));

        Jobs.Clear();
        foreach (var job in earnings.Jobs)
            Jobs.Add(new DriverJobCardItem(job, j => Navigation.GoToAsync(Routes.DriverJobDetail, Param(Routes.JobIdKey, j.Id))));
        HasJobs = Jobs.Count > 0;
        return true;
    }

    private void OnPeriodSelected(SegmentItem item)
    {
        foreach (var p in Periods)
            p.IsSelected = ReferenceEquals(p, item);
        _period = Enum.Parse<EarningsPeriod>(item.Key);
        _ = LoadAsync(showSkeleton: false);
    }
}

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

/// <summary>Edit name, contact details and driver's licence number.</summary>
public sealed partial class DriverEditProfileViewModel(ViewModelServices services, IDriverService drivers)
    : BaseViewModel(services)
{
    private static readonly Regex EmailPattern = new(@"^[^@\s]+@[^@\s]+\.[^@\s]+$", RegexOptions.Compiled);

    [ObservableProperty] private string name = string.Empty;
    [ObservableProperty] private string email = string.Empty;
    [ObservableProperty] private string phone = string.Empty;
    [ObservableProperty] private string licenseNumber = string.Empty;
    [ObservableProperty] private string validationMessage = string.Empty;
    [ObservableProperty] private bool hasValidationMessage;

    protected override async Task<bool> LoadDataAsync()
    {
        var profile = await drivers.GetProfileAsync();
        Name = profile.User.Name;
        Email = profile.User.Email;
        Phone = profile.User.Phone;
        LicenseNumber = profile.Driver.LicenseNumber;
        return true;
    }

    public override Task OnAppearingAsync() => IsContent ? Task.CompletedTask : base.OnAppearingAsync();

    [RelayCommand]
    private Task SaveAsync() => RunBusyAsync(async () =>
    {
        var error = Validate();
        ValidationMessage = error ?? string.Empty;
        HasValidationMessage = error is not null;
        if (error is not null)
            return;

        await drivers.UpdateProfileAsync(Name.Trim(), Email.Trim(), Phone.Trim(), LicenseNumber.Trim());
        await Navigation.GoBackAsync();
    });

    private string? Validate()
    {
        if (string.IsNullOrWhiteSpace(Name) || Name.Trim().Length < 2)
            return "Please enter your full name.";
        if (string.IsNullOrWhiteSpace(Phone) || Phone.Count(char.IsDigit) < 9)
            return "A valid mobile number is required so customers and providers can reach you.";
        if (!string.IsNullOrWhiteSpace(Email) && !EmailPattern.IsMatch(Email.Trim()))
            return "Please enter a valid email address.";
        if (string.IsNullOrWhiteSpace(LicenseNumber) || LicenseNumber.Trim().Length < 5)
            return "Please enter your driver's licence number.";
        return null;
    }
}
