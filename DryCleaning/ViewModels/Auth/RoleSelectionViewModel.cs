using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using DryCleaning.Helpers;
using DryCleaning.Models;
using DryCleaning.Services;
using DryCleaning.Services.Mock;

namespace DryCleaning.ViewModels.Auth;

public sealed partial class RoleSelectionViewModel(ViewModelServices services, ISessionService session) : BaseViewModel(services)
{
    /// <summary>Debug builds show which environment (set-app-env) and server the app is using.</summary>
    public bool ShowEnvironment =>
#if DEBUG
        true;
#else
        false;
#endif

    public string EnvironmentText
    {
        get
        {
            var settings = Services.Settings;
            var name = string.IsNullOrWhiteSpace(settings.Environment) ? "appsettings.json" : settings.Environment;
            var target = settings.Features.UseMockData ? "demo data" : settings.Api.ResolveBaseUrl();
            return $"Environment: {name} · {target}";
        }
    }

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

        if (selected is not (UserRole.Provider or UserRole.Customer or UserRole.Driver))
        {
            await Dialogs.AlertAsync("Not available", $"The {selected} role uses the EDry Cleaners admin portal.");
            return;
        }

        session.SelectedRole = selected;
        await Navigation.GoToAsync(Routes.Login);
    }
}
