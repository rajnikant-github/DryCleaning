using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using DryCleaning.Helpers;
using DryCleaning.Models;
using DryCleaning.Services;
using DryCleaning.ViewModels.Items;

namespace DryCleaning.ViewModels.Driver;

/// <summary>D07 – Pickup complete → navigate to the provider.</summary>
public sealed partial class DriverPickupCompleteViewModel(ViewModelServices services, IDriverService drivers)
    : DriverJobViewModelBase(services, drivers)
{
    protected override void OnDetailLoaded(DriverJobDetail d) => Title = "Pickup Complete";

    [RelayCommand]
    private Task NavigateToProviderAsync() => ReplaceWithAsync(Routes.DriverNavigate);
}
