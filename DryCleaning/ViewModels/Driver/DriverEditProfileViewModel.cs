using System.Collections.ObjectModel;
using System.Text.RegularExpressions;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using DryCleaning.Helpers;
using DryCleaning.Models;
using DryCleaning.Services;
using DryCleaning.ViewModels.Items;

namespace DryCleaning.ViewModels.Driver;

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
