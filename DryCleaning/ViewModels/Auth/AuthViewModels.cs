using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using DryCleaning.Helpers;
using DryCleaning.Models;
using DryCleaning.Services;
using DryCleaning.Services.Mock;

namespace DryCleaning.ViewModels.Auth;

public sealed class SplashViewModel(ViewModelServices services, ISessionService session) : BaseViewModel(services)
{
    public override async Task OnAppearingAsync()
    {
        State = ViewState.Content;
        await Task.Delay(1600);
        await Navigation.GoToAsync(session.CurrentUser is { } user ? Routes.HomeFor(user.Role) : Routes.RoleSelection);
    }
}

public sealed partial class RoleSelectionViewModel(ViewModelServices services, ISessionService session) : BaseViewModel(services)
{
    public override Task OnAppearingAsync()
    {
        State = ViewState.Content;
        return Task.CompletedTask;
    }

    [RelayCommand]
    private async Task SelectRoleAsync(string role)
    {
        if (!Enum.TryParse<UserRole>(role, out var selected))
            return;

        if (selected is not (UserRole.Provider or UserRole.Customer))
        {
            await Dialogs.AlertAsync("Coming soon",
                $"The {selected} app is being built next. Please continue as a Customer or Service Provider for now.");
            return;
        }

        session.SelectedRole = selected;
        await Navigation.GoToAsync(Routes.Login);
    }
}

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

public sealed partial class OtpViewModel(ViewModelServices services, IAuthService auth, ISessionService session)
    : BaseViewModel(services), IQueryAttributable
{
    private const int ResendSeconds = 30;
    private CancellationTokenSource? _countdown;
    private string _identifier = string.Empty;

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(VerifyCommand))]
    private string code = string.Empty;

    [ObservableProperty]
    private string destination = string.Empty;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(CanResend))]
    [NotifyPropertyChangedFor(nameof(ResendText))]
    private int secondsRemaining;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(HasError))]
    private string errorText = string.Empty;

    public bool HasError => !string.IsNullOrEmpty(ErrorText);
    public bool CanResend => SecondsRemaining == 0;
    public string ResendText => CanResend ? "Resend OTP" : $"Resend OTP in {SecondsRemaining}s";
    public bool ShowDemoHint => Services.Settings.Features.UseMockData;
    public string DemoHint => $"Demo code: {MockAuthService.DemoOtp}";

    public void ApplyQueryAttributes(IDictionary<string, object> query)
    {
        if (query.TryGetValue(Routes.IdentifierKey, out var value) && value is string identifier)
            _identifier = identifier;
    }

    public override async Task OnAppearingAsync()
    {
        State = ViewState.Content;
        if (string.IsNullOrEmpty(Destination))
            await SendCodeAsync();
    }

    private bool CanVerify() => Code.Length == 6;

    [RelayCommand(CanExecute = nameof(CanVerify))]
    private Task VerifyAsync() => RunBusyAsync(async () =>
    {
        ErrorText = string.Empty;
        var result = await auth.VerifyOtpAsync(_identifier, Code, session.SelectedRole);
        if (!result.Success)
        {
            ErrorText = result.Error ?? "Verification failed.";
            Code = string.Empty;
            return;
        }

        _countdown?.Cancel();
        await Navigation.GoToAsync(Routes.HomeFor(session.CurrentUser?.Role ?? session.SelectedRole));
    });

    [RelayCommand]
    private async Task ResendAsync()
    {
        if (!CanResend)
            return;

        await SendCodeAsync();
    }

    private async Task SendCodeAsync()
    {
        Destination = await auth.RequestOtpAsync(_identifier, session.SelectedRole);
        await StartCountdownAsync();
    }

    private async Task StartCountdownAsync()
    {
        _countdown?.Cancel();
        _countdown = new CancellationTokenSource();
        var token = _countdown.Token;

        SecondsRemaining = ResendSeconds;
        try
        {
            while (SecondsRemaining > 0)
            {
                await Task.Delay(1000, token);
                SecondsRemaining--;
            }
        }
        catch (TaskCanceledException)
        {
            // page left or code resent
        }
    }
}
