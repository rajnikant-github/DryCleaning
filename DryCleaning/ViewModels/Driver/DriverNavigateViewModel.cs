using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using DryCleaning.Helpers;
using DryCleaning.Models;
using DryCleaning.Services;
using DryCleaning.ViewModels.Items;

namespace DryCleaning.ViewModels.Driver;

/// <summary>D04 / D10 – Navigate to the next stop, then "I've arrived".</summary>
public sealed partial class DriverNavigateViewModel(ViewModelServices services, IDriverService drivers)
    : DriverJobViewModelBase(services, drivers)
{
    [ObservableProperty] private string headingText = string.Empty;
    [ObservableProperty] private string destinationLabel = string.Empty;
    [ObservableProperty] private string arrivedText = "I've Arrived";
    [ObservableProperty] private string stepHint = string.Empty;

    protected override void OnDetailLoaded(DriverJobDetail d)
    {
        var toCustomer = d.NextStep is DriverJobStep.CollectFromCustomer or DriverJobStep.DeliverToCustomer;
        HeadingText = toCustomer ? "Heading to the customer" : "Heading to the provider";
        DestinationLabel = d.NextStep switch
        {
            DriverJobStep.CollectFromCustomer => "Collect from",
            DriverJobStep.DropAtProvider => "Drop off at",
            DriverJobStep.CollectFromProvider => "Collect from",
            _ => "Deliver to"
        };
        Title = toCustomer ? "Navigate to Customer" : "Navigate to Provider";
        StepHint = DriverText.StepHint(d.NextStep);
    }

    [RelayCommand]
    private async Task OpenMapsAsync()
    {
        if (Detail is null)
            return;

        var destination = Detail.Destination;
        try
        {
            await Microsoft.Maui.ApplicationModel.Map.Default.OpenAsync(
                new Microsoft.Maui.Devices.Sensors.Location((double)destination.Latitude, (double)destination.Longitude),
                new Microsoft.Maui.ApplicationModel.MapLaunchOptions { Name = DestinationName, NavigationMode = Microsoft.Maui.ApplicationModel.NavigationMode.Driving });
        }
        catch (Exception)
        {
            await Dialogs.AlertAsync("Maps", $"Couldn't open a maps app. Address:\n{DestinationAddress}");
        }
    }

    [RelayCommand]
    private Task ArrivedAsync()
    {
        var route = ArrivalRoute(Step);
        return route is null ? Navigation.GoBackAsync() : ReplaceWithAsync(route);
    }
}
