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
