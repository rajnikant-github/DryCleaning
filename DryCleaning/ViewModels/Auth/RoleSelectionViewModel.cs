using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using DryCleaning.Helpers;
using DryCleaning.Models;
using DryCleaning.Services;
using DryCleaning.Services.Mock;

namespace DryCleaning.ViewModels.Auth;

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

        if (selected is not (UserRole.Provider or UserRole.Customer or UserRole.Driver))
        {
            await Dialogs.AlertAsync("Not available", $"The {selected} role uses the EDry Cleaners admin portal.");
            return;
        }

        session.SelectedRole = selected;
        await Navigation.GoToAsync(Routes.Login);
    }
}
