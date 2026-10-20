using CommunityToolkit.Mvvm.Input;
using DryCleaning.Helpers;
using DryCleaning.Services;

namespace DryCleaning.ViewModels.Onboarding;

/// <summary>Welcome screen shown to people who aren't signed in: start the onboarding, or go straight to log in.</summary>
public sealed partial class IntroViewModel(ViewModelServices services, IOnboardingService onboarding) : BaseViewModel(services)
{
    public override Task OnAppearingAsync()
    {
        State = ViewState.Content;
        return Task.CompletedTask;
    }

    [RelayCommand]
    private Task GetStartedAsync() => Navigation.GoToAsync(Routes.Onboarding);

    [RelayCommand]
    private Task LogInAsync()
    {
        // Someone with an account doesn't need the walkthrough next time.
        onboarding.MarkDone();
        return Navigation.GoToAsync(Routes.RoleSelection);
    }
}
