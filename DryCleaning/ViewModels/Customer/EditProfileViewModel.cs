using System.Text.RegularExpressions;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using DryCleaning.Helpers;
using DryCleaning.Services;

namespace DryCleaning.ViewModels.Customer;

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
