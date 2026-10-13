using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using DryCleaning.Helpers;
using DryCleaning.Models;
using DryCleaning.Services;
using DryCleaning.Services.Mock;

namespace DryCleaning.ViewModels.Auth;

public sealed partial class LoginViewModel(ViewModelServices services, IAuthService auth, ISessionService session) : BaseViewModel(services)
{
    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(LoginCommand))]
    [NotifyCanExecuteChangedFor(nameof(ContinueWithOtpCommand))]
    private string identifier = string.Empty;

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(LoginCommand))]
    private string password = string.Empty;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(PasswordToggleText))]
    private bool isPasswordHidden = true;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(HasError))]
    private string errorText = string.Empty;

    public bool HasError => !string.IsNullOrEmpty(ErrorText);
    public string PasswordToggleText => IsPasswordHidden ? "Show" : "Hide";
    public string RoleTitle => session.SelectedRole switch
    {
        UserRole.Provider => "Service Provider Login",
        UserRole.Customer => "Customer Login",
        var role => $"{role} Login"
    };

    public override Task OnAppearingAsync()
    {
        State = ViewState.Content;
        OnPropertyChanged(nameof(RoleTitle));
#if DEBUG
        // Debug builds: demo account for the selected role (seeded by the API, password Demo@12345).
        (Identifier, Password) = session.SelectedRole switch
        {
            UserRole.Customer => ("sipho@example.co.za", "Demo@12345"),
            UserRole.Provider => ("thandi@sparkledrycleaners.co.za", "Demo@12345"),
            UserRole.Driver => ("bongani@drivers.edrycleaners.co.za", "Demo@12345"),
            _ => (string.Empty, string.Empty)
        };
#else
        // Release builds: the user types their own credentials.
#endif
        return Task.CompletedTask;
    }

    private bool CanLogin() => !string.IsNullOrWhiteSpace(Identifier) && !string.IsNullOrWhiteSpace(Password);
    private bool CanUseOtp() => !string.IsNullOrWhiteSpace(Identifier);

    [RelayCommand(CanExecute = nameof(CanLogin))]
    private Task LoginAsync() => RunBusyAsync(async () =>
    {
        ErrorText = string.Empty;
        var result = await auth.LoginAsync(Identifier.Trim(), Password, session.SelectedRole);
        if (!result.Success)
        {
            ErrorText = result.Error ?? "Login failed.";
            return;
        }

        Password = string.Empty;
        await Navigation.GoToAsync(Routes.HomeFor(session.CurrentUser?.Role ?? session.SelectedRole));
    });

    [RelayCommand(CanExecute = nameof(CanUseOtp))]
    private Task ContinueWithOtpAsync() => RunBusyAsync(async () =>
    {
        ErrorText = string.Empty;
        await Navigation.GoToAsync(Routes.Otp, Param(Routes.IdentifierKey, Identifier.Trim()));
    });

    [RelayCommand]
    private async Task ForgotPasswordAsync()
    {
        var value = Identifier;
        if (string.IsNullOrWhiteSpace(value))
        {
            value = await Dialogs.PromptAsync("Reset password", "Enter your mobile number or email. We'll send you a 6-digit code.", "Send code", "+27 82 555 0142") ?? string.Empty;
            if (string.IsNullOrWhiteSpace(value))
                return;
            Identifier = value;
        }

        await Navigation.GoToAsync(Routes.Otp, Param(Routes.IdentifierKey, value.Trim()));
    }

    [RelayCommand]
    private Task CreateAccountAsync() =>
        Dialogs.AlertAsync("Become a provider",
            "Service Provider accounts are verified by the EDry Cleaners team. Please email partners@edrycleaners.co.za to register your business.");

    [RelayCommand]
    private void TogglePassword() => IsPasswordHidden = !IsPasswordHidden;

    [RelayCommand]
    private Task ChangeRoleAsync() => Navigation.GoToAsync(Routes.RoleSelection);
}
