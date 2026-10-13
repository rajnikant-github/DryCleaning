using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using DryCleaning.Helpers;
using DryCleaning.Models;
using DryCleaning.Services;
using DryCleaning.ViewModels.Items;

namespace DryCleaning.ViewModels.Provider;

/// <summary>P08 – Quality check before delivery.</summary>
public sealed partial class QualityCheckViewModel : OrderPageViewModelBase
{
    private readonly IPhotoService _photos;

    public QualityCheckViewModel(ViewModelServices services, IProviderOrderService orderService, IPhotoService photos)
        : base(services, orderService)
    {
        _photos = photos;
        foreach (var text in new[] { "Cleaning completed", "Stains checked", "Items counted", "Packaging completed" })
            Checklist.Add(new ChecklistItem(text, ChecklistChanged));
    }

    public ObservableCollection<ChecklistItem> Checklist { get; } = [];
    public ObservableCollection<string> Photos { get; } = [];

    [ObservableProperty] private bool isInQualityCheck;
    [ObservableProperty] private string statusHint = string.Empty;

    public string ProgressText => $"{Checklist.Count(c => c.IsChecked)} of {Checklist.Count} checks complete";

    private void ChecklistChanged()
    {
        OnPropertyChanged(nameof(ProgressText));
        MarkReadyCommand.NotifyCanExecuteChanged();
    }

    protected override void OnDetailLoaded(OrderDetail detail)
    {
        IsInQualityCheck = detail.Order.Status == OrderStatus.QualityCheck;
        StatusHint = IsInQualityCheck ? string.Empty : $"This order is '{detail.Order.Status.DisplayName()}'.";
        MarkReadyCommand.NotifyCanExecuteChanged();
        SendBackCommand.NotifyCanExecuteChanged();
    }

    private bool CanMarkReady() => IsInQualityCheck && Checklist.All(c => c.IsChecked);
    private bool CanSendBack() => IsInQualityCheck;

    [RelayCommand]
    private async Task AddPhotoAsync()
    {
        var path = await _photos.AddPhotoAsync();
        if (path is not null)
            Photos.Add(path);
    }

    [RelayCommand(CanExecute = nameof(CanMarkReady))]
    private Task MarkReadyAsync() => RunBusyAsync(async () =>
    {
        if (Photos.Count == 0 &&
            !await Dialogs.ConfirmAsync("No photos", "Add a photo of the packed order? It helps resolve delivery disputes.", "Continue without", "Add photo"))
        {
            return;
        }

        await OrderService.MarkReadyForDeliveryAsync(OrderId, Photos.ToList());
        await ReplaceWithAsync(Routes.ReadyForDelivery);
    });

    [RelayCommand(CanExecute = nameof(CanSendBack))]
    private Task SendBackAsync() => RunBusyAsync(async () =>
    {
        var reason = await Dialogs.PromptAsync("Needs re-cleaning", "What needs to be fixed?", "Send back");
        if (string.IsNullOrWhiteSpace(reason))
            return;

        await OrderService.SendBackToProcessingAsync(OrderId, reason.Trim());
        await ReplaceWithAsync(Routes.Processing);
    });
}
