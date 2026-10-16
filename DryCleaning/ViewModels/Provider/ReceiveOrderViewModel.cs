using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using DryCleaning.Helpers;
using DryCleaning.Models;
using DryCleaning.Services;
using DryCleaning.ViewModels.Items;

namespace DryCleaning.ViewModels.Provider;

/// <summary>P05 – Receive order from the pickup driver.</summary>
public sealed partial class ReceiveOrderViewModel : OrderPageViewModelBase
{
    private readonly IPhotoService _photos;

    public ReceiveOrderViewModel(ViewModelServices services, IProviderOrderService orderService, IPhotoService photos)
        : base(services, orderService)
    {
        _photos = photos;
        Checklist.Add(new ChecklistItem("Item count matches the order", ChecklistChanged));
        Checklist.Add(new ChecklistItem("Bags and packaging intact", ChecklistChanged));
        Checklist.Add(new ChecklistItem("Order tag / label attached", ChecklistChanged));
    }

    public ObservableCollection<ChecklistItem> Checklist { get; } = [];
    public ObservableCollection<string> Photos { get; } = [];

    [ObservableProperty] private int expectedCount;
    [ObservableProperty] private string driverText = string.Empty;
    [ObservableProperty] private bool isReceivable;
    [ObservableProperty] private string notReceivableText = string.Empty;
    [ObservableProperty] private string notes = string.Empty;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(HasDiscrepancy))]
    [NotifyPropertyChangedFor(nameof(DiscrepancyText))]
    private int actualCount;

    [ObservableProperty]
    private bool discrepancyReported;

    public bool HasDiscrepancy => ActualCount != ExpectedCount;
    public string DiscrepancyText => ActualCount < ExpectedCount
        ? $"{ExpectedCount - ActualCount} item(s) missing"
        : $"{ActualCount - ExpectedCount} extra item(s)";

    public bool AllChecked => Checklist.All(c => c.IsChecked);

    private void ChecklistChanged()
    {
        OnPropertyChanged(nameof(AllChecked));
        ConfirmReceivedCommand.NotifyCanExecuteChanged();
    }

    protected override void OnDetailLoaded(OrderDetail detail)
    {
        ExpectedCount = detail.ExpectedItemCount;
        if (ActualCount == 0)
            ActualCount = ExpectedCount;

        DriverText = detail.PickupDriver is null
            ? "Driver details unavailable"
            : string.IsNullOrWhiteSpace(detail.PickupDriver.LicenseNumber)
                ? detail.PickupDriver.DriverName
                : $"{detail.PickupDriver.DriverName} · {detail.PickupDriver.LicenseNumber}";

        IsReceivable = detail.Order.Status == OrderStatus.PickedUp;
        NotReceivableText = IsReceivable ? string.Empty
            : $"This order is '{detail.Order.Status.DisplayName()}' and can't be received right now.";
        ConfirmReceivedCommand.NotifyCanExecuteChanged();
    }

    private bool CanConfirm() => IsReceivable && AllChecked;

    [RelayCommand]
    private async Task AddPhotoAsync()
    {
        var path = await _photos.AddPhotoAsync();
        if (path is not null)
            Photos.Add(path);
    }

    [RelayCommand]
    private Task ReportDiscrepancyAsync() => RunBusyAsync(async () =>
    {
        if (await ReportItemIssueAsync("Quantity discrepancy"))
            DiscrepancyReported = true;
    });

    [RelayCommand(CanExecute = nameof(CanConfirm))]
    private Task ConfirmReceivedAsync() => RunBusyAsync(async () =>
    {
        if (HasDiscrepancy && !DiscrepancyReported)
        {
            var report = await Dialogs.ConfirmAsync("Counts don't match",
                $"You received {ActualCount} of {ExpectedCount} items. Report a discrepancy before continuing?",
                "Report", "Continue anyway");
            if (report)
            {
                if (await ReportItemIssueAsync("Quantity discrepancy"))
                    DiscrepancyReported = true;
                return;
            }
        }

        await OrderService.ConfirmReceivedAsync(OrderId, new ReceiveOrderRequest
        {
            ActualItemCount = ActualCount,
            Notes = Notes,
            PhotoPaths = Photos.ToList()
        });

        await ReplaceWithAsync(Routes.Inspection);
    });
}
