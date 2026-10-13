using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using DryCleaning.Helpers;
using DryCleaning.Services;

namespace DryCleaning.ViewModels.Auth;

/// <summary>
/// Reset a forgotten password: 1) send a code to the mobile number / email, 2) enter the code and a new password.
/// A successful reset signs the user in (other devices are signed out).
/// </summary>
public sealed partial class ForgotPasswordViewModel(ViewModelServices services, IAuthService auth, ISessionService session)
    : BaseViewModel(services), IQueryAttributable
{
    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(SendCodeCommand))]
    private string identifier = string.Empty;

    [ObservableProperty] private string destination = string.Empty;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsRequestStep))]
    private bool isCodeSent;

    [ObservableProperty] private string code = string.Empty;
    [ObservableProperty] private string newPassword = string.Empty;
    [ObservableProperty] private string confirmPassword = string.Empty;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(PasswordToggleText))]
    private bool isPasswordHidden = true;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(HasError))]
    private string errorText = string.Empty;

    public bool IsRequestStep => !IsCodeSent;
    public bool HasError => !string.IsNullOrEmpty(ErrorText);
    public string PasswordToggleText => IsPasswordHidden ? "Show" : "Hide";
    public string PasswordHint => InputRules.PasswordHint;
    public bool ShowCodeHint => IsCodeSent && !string.IsNullOrEmpty(auth.OtpHint);
    public string CodeHint => $"Demo code: {auth.OtpHint}";

    public void ApplyQueryAttributes(IDictionary<string, object> query)
    {
        if (query.TryGetValue(Routes.IdentifierKey, out var value) && value is string id && string.IsNullOrEmpty(Identifier))
            Identifier = id;
    }

    public override Task OnAppearingAsync()
    {
        State = ViewState.Content;
        return Task.CompletedTask;
    }

    private bool CanSendCode() => !string.IsNullOrWhiteSpace(Identifier);

    [RelayCommand(CanExecute = nameof(CanSendCode))]
    private Task SendCodeAsync() => RunBusyAsync(async () =>
    {
        ErrorText = string.Empty;
        try
        {
            Destination = await auth.RequestPasswordResetAsync(Identifier.Trim());
            Code = string.Empty;
            IsCodeSent = true;
        }
        catch (Exception ex)
        {
            ErrorText = ex.Message;
        }

        OnPropertyChanged(nameof(ShowCodeHint));
        OnPropertyChanged(nameof(CodeHint));
    });

    [RelayCommand]
    private Task ResetAsync() => RunBusyAsync(async () =>
    {
        ErrorText = string.Empty;
        if (Code.Trim().Length != 6)
        {
            ErrorText = "Enter the 6-digit code we sent you.";
            return;
        }

        if (InputRules.PasswordError(NewPassword) is { } passwordError)
        {
            ErrorText = passwordError;
            return;
        }

        if (NewPassword != ConfirmPassword)
        {
            ErrorText = "The passwords don't match.";
            return;
        }

        var result = await auth.ResetPasswordAsync(Identifier.Trim(), Code.Trim(), NewPassword, session.SelectedRole);
        if (!result.Success)
        {
            ErrorText = result.Error ?? "We couldn't reset your password. Please try again.";
            return;
        }

        NewPassword = ConfirmPassword = string.Empty;
        await Dialogs.AlertAsync("Password changed", "Your new password is set and you're signed in.");
        await Navigation.GoToAsync(Routes.HomeFor(session.CurrentUser?.Role ?? session.SelectedRole));
    });

    [RelayCommand]
    private void ChangeIdentifier()
    {
        IsCodeSent = false;
        ErrorText = string.Empty;
        OnPropertyChanged(nameof(ShowCodeHint));
    }

    [RelayCommand]
    private void TogglePassword() => IsPasswordHidden = !IsPasswordHidden;
}
