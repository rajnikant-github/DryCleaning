using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using DryCleaning.Helpers;
using DryCleaning.Models;
using DryCleaning.Services;
using DryCleaning.ViewModels.Items;

namespace DryCleaning.ViewModels.Driver;

/// <summary>D08 – Hand the items over to the provider (pickup job completed).</summary>
public sealed partial class DriverHandoverViewModel(ViewModelServices services, IDriverService drivers, IPhotoService photos)
    : DriverJobViewModelBase(services, drivers)
{
    public ObservableCollection<OrderItemDetail> Items { get; } = [];
    public ObservableCollection<string> Photos { get; } = [];

    [ObservableProperty] private string notes = string.Empty;

    public override Task OnAppearingAsync() => IsContent ? Task.CompletedTask : base.OnAppearingAsync();

    protected override void OnDetailLoaded(DriverJobDetail d)
    {
        Title = "Handover to Provider";
        Items.Clear();
        foreach (var item in d.Items)
            Items.Add(item);
    }

    [RelayCommand]
    private async Task AddPhotoAsync()
    {
        var path = await photos.AddPhotoAsync();
        if (path is not null)
            Photos.Add(path);
    }

    [RelayCommand]
    private Task ConfirmAsync() => RunBusyAsync(async () =>
    {
        if (!await Dialogs.ConfirmAsync("Confirm handover",
                $"Confirm you've handed {ItemCountText} for {OrderNumber} to {ProviderName}?", "Confirm", "Cancel"))
            return;

        await Drivers.ConfirmHandoverAsync(JobId, string.IsNullOrWhiteSpace(Notes) ? null : Notes.Trim(), Photos.ToList());
        await ReplaceWithAsync(Routes.DriverJobCompleted);
    });
}
