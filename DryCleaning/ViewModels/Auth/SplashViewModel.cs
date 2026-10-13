using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using DryCleaning.Helpers;
using DryCleaning.Models;
using DryCleaning.Services;
using DryCleaning.Services.Mock;

namespace DryCleaning.ViewModels.Auth;

public sealed class SplashViewModel(ViewModelServices services, ISessionService session, IAuthService auth) : BaseViewModel(services)
{
    public override async Task OnAppearingAsync()
    {
        State = ViewState.Content;
        // Show the splash for at least 1.6s while a saved session (API mode) is restored.
        await Task.WhenAll(Task.Delay(1600), auth.RestoreSessionAsync());
        await Navigation.GoToAsync(session.CurrentUser is { } user ? Routes.HomeFor(user.Role) : Routes.RoleSelection);
    }
}
