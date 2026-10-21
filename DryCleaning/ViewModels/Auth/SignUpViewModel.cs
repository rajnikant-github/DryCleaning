using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using DryCleaning.Helpers;
using DryCleaning.Models;
using DryCleaning.Services;
using DryCleaning.ViewModels.Items;

namespace DryCleaning.ViewModels.Auth;

/// <summary>Create a Customer, Service Provider or Driver account (role chosen on the Role screen).</summary>
public sealed partial class SignUpViewModel(
    ViewModelServices services,
    IAuthService auth,
    ISessionService session,
    ICustomerCatalogService catalog) : BaseViewModel(services)
{
    public ObservableCollection<CategoryToggleItem> Categories { get; } = [];

    [ObservableProperty] private string name = string.Empty;
    [ObservableProperty] private string email = string.Empty;
    [ObservableProperty] private string phone = string.Empty;
    [ObservableProperty] private string password = string.Empty;
    [ObservableProperty] private string confirmPassword = string.Empty;
    [ObservableProperty] private string licenseNumber = string.Empty;
    [ObservableProperty] private string businessName = string.Empty;
    [ObservableProperty] private string description = string.Empty;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(PasswordToggleText))]
    private bool isPasswordHidden = true;

    [ObservableProperty] private string validationMessage = string.Empty;
    [ObservableProperty] private bool hasValidationMessage;

    public UserRole Role => session.SelectedRole;
    public bool IsDriver => Role == UserRole.Driver;
    public bool IsProvider => Role == UserRole.Provider;
    public string PasswordToggleText => IsPasswordHidden ? "Show" : "Hide";
    public string PasswordHint => InputRules.PasswordHint;

    public string Heading => Role switch
    {
        UserRole.Provider => "Register your business",
        UserRole.Driver => "Become a driver",
        _ => "Create your account"
    };

    public string Subheading => Role switch
    {
        UserRole.Provider => "Tell us about your business. Our team reviews every new provider before it goes live.",
        UserRole.Driver => "Pick up and deliver orders for dry cleaners near you.",
        _ => "Book pickups and track your laundry in one place."
    };

    protected override async Task<bool> LoadDataAsync()
    {
        OnPropertyChanged(nameof(Heading));
        OnPropertyChanged(nameof(Subheading));
        OnPropertyChanged(nameof(IsDriver));
        OnPropertyChanged(nameof(IsProvider));

        if (IsProvider && Categories.Count == 0)
        {
            foreach (var category in await catalog.GetCategoriesAsync())
                Categories.Add(new CategoryToggleItem(category.Id, category.Name, category.Description, isSelected: false));
        }

        return true;
    }

    // Load once – don't wipe what the user typed when the page re-appears.
    public override Task OnAppearingAsync() => IsContent ? Task.CompletedTask : base.OnAppearingAsync();

    [RelayCommand]
    private void TogglePassword() => IsPasswordHidden = !IsPasswordHidden;

    [RelayCommand]
    private Task CreateAccountAsync() => RunBusyAsync(async () =>
    {
        var error = Validate();
        ShowError(error);
        if (error is not null)
            return;

        var email = Email.Trim();
        var result = await auth.StartRegistrationAsync(new Registration
        {
            Role = Role,
            Name = Name.Trim(),
            Email = email,
            Phone = string.IsNullOrWhiteSpace(Phone) ? null : Phone.Trim(),
            Password = Password,
            LicenseNumber = IsDriver ? LicenseNumber.Trim() : null,
            BusinessName = IsProvider ? BusinessName.Trim() : null,
            Description = IsProvider && !string.IsNullOrWhiteSpace(Description) ? Description.Trim() : null,
            CategoryIds = IsProvider ? Categories.Where(c => c.IsSelected).Select(c => c.Id).ToList() : []
        });

        if (!result.Success)
        {
            ShowError(result.Error ?? "We couldn't start your sign-up. Please try again.");
            return;
        }

        // No account exists yet: the next screen asks for the code we just emailed and creates it.
        await Navigation.GoToAsync(Routes.Otp, new Dictionary<string, object>
        {
            [Routes.IdentifierKey] = email,
            [Routes.DestinationKey] = result.Destination,
            [Routes.PurposeKey] = Routes.RegisterPurpose,
            [Routes.ResendSecondsKey] = result.ResendInSeconds
        });
    });

    [RelayCommand]
    private Task SignInInsteadAsync() => Navigation.GoBackAsync();

    private void ShowError(string? error)
    {
        ValidationMessage = error ?? string.Empty;
        HasValidationMessage = error is not null;
    }

    private string? Validate()
    {
        if (string.IsNullOrWhiteSpace(Name) || Name.Trim().Length < 2)
            return "Please enter your full name.";
        if (string.IsNullOrWhiteSpace(Email))
            return "Enter your email address – we'll email you a code to confirm it.";
        if (!InputRules.IsValidEmail(Email))
            return "Please enter a valid email address.";
        if (!string.IsNullOrWhiteSpace(Phone) && !InputRules.IsValidPhone(Phone))
            return "Please enter a valid mobile number, e.g. +27 82 123 4567.";

        if (IsDriver && (string.IsNullOrWhiteSpace(LicenseNumber) || LicenseNumber.Trim().Length < 5))
            return "Please enter your driver's licence number.";

        if (IsProvider)
        {
            if (string.IsNullOrWhiteSpace(BusinessName) || BusinessName.Trim().Length < 2)
                return "Please enter your business name.";
            if (!Categories.Any(c => c.IsSelected))
                return "Choose at least one category you offer.";
        }

        if (InputRules.PasswordError(Password) is { } passwordError)
            return passwordError;
        if (Password != ConfirmPassword)
            return "The passwords don't match.";

        return null;
    }
}
