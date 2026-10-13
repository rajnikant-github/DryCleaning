using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using DryCleaning.Configuration;
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

    /// <summary>Debug builds: shown while the fields hold the demo account from the environment profile.</summary>
    [ObservableProperty]
    private bool isDebugLoginFilled;

    public override Task OnAppearingAsync()
    {
        State = ViewState.Content;
        OnPropertyChanged(nameof(RoleTitle));
#if DEBUG
        FillDebugLogin();
#endif
        return Task.CompletedTask;
    }

#if DEBUG
    private DebugLogin? _autoFilled;

    /// <summary>
    /// Fills the selected role's demo account (DebugLogins in environments/local.json or mock.json).
    /// Never overwrites what the user typed; switching role swaps one demo account for another.
    /// </summary>
    private void FillDebugLogin()
    {
        var untouched = (string.IsNullOrEmpty(Identifier) || Identifier == _autoFilled?.Identifier) &&
                        (string.IsNullOrEmpty(Password) || Password == _autoFilled?.Password);
        if (!untouched)
            return;

        var login = Services.Settings.DebugLogins.For(session.SelectedRole);
        if (login is null)
        {
            if (_autoFilled is not null)
            {
                Identifier = string.Empty;
                Password = string.Empty;
                _autoFilled = null;
            }

            IsDebugLoginFilled = false;
            return;
        }

        Identifier = login.Identifier;
        Password = login.Password;
        _autoFilled = login;
        IsDebugLoginFilled = true;
    }

    partial void OnIdentifierChanged(string value) => UpdateDebugHint();

    partial void OnPasswordChanged(string value) => UpdateDebugHint();

    private void UpdateDebugHint() =>
        IsDebugLoginFilled = _autoFilled is not null && Identifier == _autoFilled.Identifier && Password == _autoFilled.Password;
#endif

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
