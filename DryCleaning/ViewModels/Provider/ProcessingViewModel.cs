using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using DryCleaning.Helpers;
using DryCleaning.Models;
using DryCleaning.Services;
using DryCleaning.ViewModels.Items;

namespace DryCleaning.ViewModels.Provider;

/// <summary>P07 – Order processing.</summary>
public sealed partial class ProcessingViewModel(ViewModelServices services, IProviderOrderService orderService)
    : OrderPageViewModelBase(services, orderService)
{
    [ObservableProperty] private string inspectionNotes = string.Empty;
    [ObservableProperty] private bool hasInspectionNotes;
    [ObservableProperty] private string startedText = string.Empty;
    [ObservableProperty] private string estimatedText = string.Empty;
    [ObservableProperty] private bool isProcessing;
    [ObservableProperty] private string statusHint = string.Empty;

    protected override void OnDetailLoaded(OrderDetail detail)
    {
        var started = detail.History.LastOrDefault(h => h.Status == OrderStatus.Processing);
        InspectionNotes = started?.Notes ?? string.Empty;
        HasInspectionNotes = !string.IsNullOrWhiteSpace(InspectionNotes);
        StartedText = started is null ? "—" : started.CreatedAt.ToString("d MMM, HH:mm");

        IsProcessing = detail.Order.Status == OrderStatus.Processing;
        StatusHint = IsProcessing
            ? "Clean all items, then send the order for quality check."
            : $"This order is '{detail.Order.Status.DisplayName()}'.";
        MarkQualityCheckCommand.NotifyCanExecuteChanged();
    }

    private bool CanMark() => IsProcessing;

    [RelayCommand(CanExecute = nameof(CanMark))]
    private Task MarkQualityCheckAsync() => RunBusyAsync(async () =>
    {
        await OrderService.MarkQualityCheckAsync(OrderId);
        await ReplaceWithAsync(Routes.QualityCheck);
    });
}
