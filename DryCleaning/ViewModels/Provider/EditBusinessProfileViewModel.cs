using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using DryCleaning.Helpers;
using DryCleaning.Models;
using DryCleaning.Services;

namespace DryCleaning.ViewModels.Provider;

/// <summary>Edit the business name, description, contact details and logo.</summary>
public sealed partial class EditBusinessProfileViewModel(
    ViewModelServices services,
    IProviderProfileService profiles,
    IPhotoService photos) : BaseViewModel(services)
{
    [ObservableProperty] private string businessName = string.Empty;
    [ObservableProperty] private string description = string.Empty;
    [ObservableProperty] private string phone = string.Empty;
    [ObservableProperty] private string email = string.Empty;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(HasLogo))]
    [NotifyPropertyChangedFor(nameof(LogoButtonText))]
    [NotifyPropertyChangedFor(nameof(LogoSource))]
    private string? logoUrl;

    [ObservableProperty] private string validationMessage = string.Empty;
    [ObservableProperty] private bool hasValidationMessage;

    public bool HasLogo => !string.IsNullOrWhiteSpace(LogoUrl);
    public string LogoButtonText => HasLogo ? "Change logo" : "Add logo";
    public ImageSource? LogoSource => ImageSources.FromPathOrUrl(LogoUrl);

    protected override async Task<bool> LoadDataAsync()
    {
        var profile = await profiles.GetProfileAsync();
        BusinessName = profile.Provider.BusinessName;
        Description = profile.Provider.Description;
        Phone = profile.Provider.Phone;
        Email = profile.Provider.Email;
        LogoUrl = profile.Provider.LogoUrl;
        return true;
    }

    // Only load once – don't overwrite the user's edits when the page re-appears (e.g. after the photo picker).
    public override Task OnAppearingAsync() => IsContent ? Task.CompletedTask : base.OnAppearingAsync();

    [RelayCommand]
    private async Task ChangeLogoAsync()
    {
        var path = await photos.AddPhotoAsync();
        if (string.IsNullOrEmpty(path))
            return;

        await RunBusyAsync(async () => LogoUrl = await profiles.UploadLogoAsync(path));
    }

    [RelayCommand]
    private Task SaveAsync() => RunBusyAsync(async () =>
    {
        var error = Validate();
        ValidationMessage = error ?? string.Empty;
        HasValidationMessage = error is not null;
        if (error is not null)
            return;

        await profiles.UpdateProfileAsync(new ProviderProfileUpdate
        {
            BusinessName = BusinessName.Trim(),
            Description = Description.Trim(),
            Phone = Phone.Trim(),
            Email = Email.Trim(),
            LogoUrl = LogoUrl
        });
        await Navigation.GoBackAsync();
    });

    private string? Validate()
    {
        if (string.IsNullOrWhiteSpace(BusinessName) || BusinessName.Trim().Length < 2)
            return "Please enter your business name.";
        if (!string.IsNullOrWhiteSpace(Email) && !InputRules.IsValidEmail(Email))
            return "Please enter a valid email address.";
        if (!string.IsNullOrWhiteSpace(Phone) && !InputRules.IsValidPhone(Phone))
            return "Please enter a valid phone number, e.g. +27 21 555 0100.";
        return null;
    }
}
