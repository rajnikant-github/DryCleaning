using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using DryCleaning.Helpers;
using DryCleaning.Models;
using DryCleaning.Services;
using DryCleaning.ViewModels.Items;

namespace DryCleaning.ViewModels.Driver;

/// <summary>D09 – Collect the packed order from the provider → OutForDelivery.</summary>
public sealed partial class DriverCollectViewModel(ViewModelServices services, IDriverService drivers, IPhotoService photos)
    : DriverJobViewModelBase(services, drivers)
{
    public ObservableCollection<ChecklistItem> Checklist { get; } = [];
    public ObservableCollection<string> Photos { get; } = [];

    [ObservableProperty] private int packageCount = 1;

    public override Task OnAppearingAsync() => IsContent ? Task.CompletedTask : base.OnAppearingAsync();

    protected override void OnDetailLoaded(DriverJobDetail d)
    {
        Title = "Collect from Provider";
        Checklist.Clear();
        Checklist.Add(new ChecklistItem($"Order number on the bag is {d.Job.OrderNumber}", OnChecklistChanged));
        Checklist.Add(new ChecklistItem($"Package holds {ItemCountText} ({d.Job.ItemsText})", OnChecklistChanged));
        Checklist.Add(new ChecklistItem("Package is sealed and undamaged", OnChecklistChanged));
        StartDeliveryCommand.NotifyCanExecuteChanged();
    }

    private void OnChecklistChanged() => StartDeliveryCommand.NotifyCanExecuteChanged();

    [RelayCommand]
    private async Task AddPhotoAsync()
    {
        var path = await photos.AddPhotoAsync();
        if (path is not null)
            Photos.Add(path);
    }

    private bool CanStart() => Checklist.Count > 0 && Checklist.All(c => c.IsChecked);

    [RelayCommand(CanExecute = nameof(CanStart))]
    private Task StartDeliveryAsync() => RunBusyAsync(async () =>
    {
        await Drivers.StartDeliveryAsync(JobId, PackageCount, Photos.ToList());
        await ReplaceWithAsync(Routes.DriverNavigate);
    });

    [RelayCommand]
    private Task ReportProblemAsync() => Dialogs.AlertAsync("Problem at the store",
        "Don't take a package that is damaged or doesn't match. Ask the provider to fix it, or call E Dry Cleaners support on +27 21 555 0100.");
}
