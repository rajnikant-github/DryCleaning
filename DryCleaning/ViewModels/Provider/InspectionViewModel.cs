using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using DryCleaning.Helpers;
using DryCleaning.Models;
using DryCleaning.Services;
using DryCleaning.ViewModels.Items;

namespace DryCleaning.ViewModels.Provider;

/// <summary>P06 – Inspect each item.</summary>
public sealed partial class InspectionViewModel(ViewModelServices services, IProviderOrderService orderService, IPhotoService photos)
    : OrderPageViewModelBase(services, orderService)
{
    public ObservableCollection<InspectionItemViewModel> InspectionItems { get; } = [];

    [ObservableProperty] private int expectedTotal;
    [ObservableProperty] private bool canInspect;
    [ObservableProperty] private string notInspectableText = string.Empty;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(TotalsMatch))]
    private int receivedTotal;

    public bool TotalsMatch => ReceivedTotal == ExpectedTotal;

    protected override void OnDetailLoaded(OrderDetail detail)
    {
        CanInspect = detail.Order.Status == OrderStatus.ReceivedByProvider;
        NotInspectableText = CanInspect ? string.Empty
            : $"This order is '{detail.Order.Status.DisplayName()}'. Inspection is only available after the order is received.";

        if (InspectionItems.Count == 0)
        {
            foreach (var item in detail.Items)
                InspectionItems.Add(new InspectionItemViewModel(item, photos.AddPhotoAsync, ReportAsync, RecalculateTotals));
        }

        ExpectedTotal = detail.ExpectedItemCount;
        RecalculateTotals();
        CompleteInspectionCommand.NotifyCanExecuteChanged();
    }

    private void RecalculateTotals() => ReceivedTotal = InspectionItems.Sum(i => i.ReceivedQuantity);

    private async Task ReportAsync(InspectionItemViewModel item, string reason)
    {
        var detail = item.HasQuantityDiscrepancy && reason != "Damage"
            ? $"{item.ProductName} ({item.ServiceItemName}): expected {item.ExpectedQuantity}, received {item.ReceivedQuantity}."
            : $"{item.ProductName} ({item.ServiceItemName}): {item.Note}";

        await RunBusyAsync(async () =>
        {
            var description = await Dialogs.PromptAsync($"Report {reason.ToLowerInvariant()}", "Add any details for support and the customer.", "Report", initialValue: detail);
            if (string.IsNullOrWhiteSpace(description))
                return;

            await OrderService.ReportIssueAsync(OrderId, reason, description.Trim());
            item.IssueReported = true;
        });
    }

    private bool CanComplete() => CanInspect;

    [RelayCommand(CanExecute = nameof(CanComplete))]
    private Task CompleteInspectionAsync() => RunBusyAsync(async () =>
    {
        var unreported = InspectionItems.Where(i => i.IsDamaged && !i.IssueReported).ToList();
        if (unreported.Count > 0)
        {
            var proceed = await Dialogs.ConfirmAsync("Damage not reported",
                "Some items are marked as damaged but haven't been reported. Continue without reporting?",
                "Continue", "Go back");
            if (!proceed)
                return;
        }

        await OrderService.CompleteInspectionAsync(OrderId, InspectionItems.Select(i => i.ToResult()).ToList());
        await ReplaceWithAsync(Routes.Processing);
    });
}
