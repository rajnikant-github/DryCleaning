using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using DryCleaning.Helpers;
using DryCleaning.Services;

namespace DryCleaning.ViewModels.Auth;

/// <summary>Change the signed-in user's password (opened from the Customer, Provider and Driver profiles).</summary>
public sealed partial class ChangePasswordViewModel(ViewModelServices services, IAuthService auth) : BaseViewModel(services)
{
    [ObservableProperty] private string currentPassword = string.Empty;
    [ObservableProperty] private string newPassword = string.Empty;
    [ObservableProperty] private string confirmPassword = string.Empty;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(PasswordToggleText))]
    private bool isPasswordHidden = true;

    [ObservableProperty] private string validationMessage = string.Empty;
    [ObservableProperty] private bool hasValidationMessage;

    public string PasswordToggleText => IsPasswordHidden ? "Show" : "Hide";
    public string PasswordHint => InputRules.PasswordHint;

    public override Task OnAppearingAsync()
    {
        State = ViewState.Content;
        return Task.CompletedTask;
    }

    [RelayCommand]
    private void TogglePassword() => IsPasswordHidden = !IsPasswordHidden;

    [RelayCommand]
    private Task SaveAsync() => RunBusyAsync(async () =>
    {
        var error = Validate();
        ValidationMessage = error ?? string.Empty;
        HasValidationMessage = error is not null;
        if (error is not null)
            return;

        try
        {
            await auth.ChangePasswordAsync(CurrentPassword, NewPassword);
        }
        catch (Exception ex)
        {
            ValidationMessage = ex.Message;
            HasValidationMessage = true;
            return;
        }

        CurrentPassword = NewPassword = ConfirmPassword = string.Empty;
        await Dialogs.AlertAsync("Password changed", "Use your new password next time you sign in.");
        await Navigation.GoBackAsync();
    });

    private string? Validate()
    {
        if (string.IsNullOrEmpty(CurrentPassword))
            return "Enter your current password.";
        if (InputRules.PasswordError(NewPassword) is { } passwordError)
            return passwordError;
        if (NewPassword == CurrentPassword)
            return "Choose a password that's different from your current one.";
        if (NewPassword != ConfirmPassword)
            return "The new passwords don't match.";
        return null;
    }
}
