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
        return Task.CompletedTask;
    }

    private bool CanLogin() => !string.IsNullOrWhiteSpace(Identifier) && !string.IsNullOrWhiteSpace(Password);

    [RelayCommand(CanExecute = nameof(CanLogin))]
    private Task LoginAsync() => RunBusyAsync(async () =>
    {
        ErrorText = string.Empty;
        if (!InputRules.IsValidEmail(Identifier))
        {
            ErrorText = "Enter a valid email address, e.g. you@example.com.";
            return;
        }

        var result = await auth.LoginAsync(Identifier.Trim(), Password, session.SelectedRole);
        if (!result.Success)
        {
            ErrorText = result.Error ?? "Login failed.";
            return;
        }

        Password = string.Empty;
        await Navigation.GoToAsync(Routes.HomeFor(session.CurrentUser?.Role ?? session.SelectedRole));
    });

    [RelayCommand]
    private Task ForgotPasswordAsync() =>
        Navigation.GoToAsync(Routes.ForgotPassword, Param(Routes.IdentifierKey, Identifier.Trim()));

    [RelayCommand]
    private Task CreateAccountAsync() => Navigation.GoToAsync(Routes.SignUp);

    [RelayCommand]
    private void TogglePassword() => IsPasswordHidden = !IsPasswordHidden;

    [RelayCommand]
    private Task ChangeRoleAsync() => Navigation.GoToAsync(Routes.RoleSelection);
}
