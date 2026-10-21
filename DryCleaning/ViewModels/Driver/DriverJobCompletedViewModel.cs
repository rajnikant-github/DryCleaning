using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using DryCleaning.Helpers;
using DryCleaning.Models;
using DryCleaning.Services;
using DryCleaning.ViewModels.Items;

namespace DryCleaning.ViewModels.Driver;

/// <summary>D12 – Job completed with earnings.</summary>
public sealed partial class DriverJobCompletedViewModel(ViewModelServices services, IDriverService drivers)
    : DriverJobViewModelBase(services, drivers)
{
    [ObservableProperty] private string headline = string.Empty;
    [ObservableProperty] private string message = string.Empty;
    [ObservableProperty] private string routeText = string.Empty;
    [ObservableProperty] private string completedText = string.Empty;
    [ObservableProperty] private decimal earningsToday;
    [ObservableProperty] private int completedToday;

    protected override async Task<bool> LoadDataAsync()
    {
        var loaded = await base.LoadDataAsync();
        var dashboard = await Drivers.GetDashboardAsync();
        EarningsToday = dashboard.EarningsToday;
        CompletedToday = dashboard.CompletedToday;
        return loaded;
    }

    protected override void OnDetailLoaded(DriverJobDetail d)
    {
        Title = "Job Completed";
        Headline = d.Job.IsPickup ? "Pickup job completed!" : "Delivery completed!";
        Message = d.Job.IsPickup
            ? $"{d.ProviderName} will confirm receipt and start cleaning."
            : $"{d.CustomerName} has their order. Thanks for delivering with E Dry Cleaners!";
        RouteText = $"{d.Job.FromName} to {d.Job.ToName}";
        CompletedText = d.Job.CompletedAt is { } at ? at.ToString("d MMM yyyy, HH:mm") : string.Empty;
    }

    [RelayCommand]
    private Task FindMoreJobsAsync() => Navigation.GoToAsync(Routes.DriverJobs, Param(Routes.FilterKey, nameof(DriverJobFilter.Available)));

    [RelayCommand]
    private Task GoToDashboardAsync() => Navigation.GoToAsync(Routes.DriverDashboard);
}
