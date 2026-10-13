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

/// <summary>P09 – Ready for delivery / request a marketplace driver.</summary>
public sealed partial class ReadyForDeliveryViewModel(ViewModelServices services, IProviderOrderService orderService)
    : OrderPageViewModelBase(services, orderService)
{
    [ObservableProperty] private string deliveryAddress = string.Empty;
    [ObservableProperty] private string itemCountText = string.Empty;
    [ObservableProperty] private bool canRequestDriver;
    [ObservableProperty] private bool isSearching;
    [ObservableProperty] private bool isWaitingForDriver;
    [ObservableProperty] private bool hasDriver;
    [ObservableProperty] private string driverName = string.Empty;
    [ObservableProperty] private string driverInfo = string.Empty;
    [ObservableProperty] private string driverInitials = string.Empty;
    [ObservableProperty] private OrderStatus currentStatus;

    protected override void OnDetailLoaded(OrderDetail detail)
    {
        DeliveryAddress = detail.PickupAddress.FormattedAddress;
        ItemCountText = detail.ExpectedItemCount == 1 ? "1 item" : $"{detail.ExpectedItemCount} items";
        CurrentStatus = detail.Order.Status;
        IsWaitingForDriver = detail.AwaitingDriverFor == DriverJobType.Delivery;
        CanRequestDriver = detail.Order.Status == OrderStatus.ReadyForDelivery && !IsWaitingForDriver;
        SetDriver(detail.DeliveryDriver);
        RequestDriverCommand.NotifyCanExecuteChanged();
    }

    private void SetDriver(DriverAssignment? driver)
    {
        HasDriver = driver is not null;
        DriverName = driver?.DriverName ?? string.Empty;
        DriverInitials = StringHelpers.Initials(driver?.DriverName);
        DriverInfo = driver is null ? string.Empty : $"{driver.LicenseNumber} · {driver.DriverPhone}";
    }

    private bool CanRequest() => CanRequestDriver && !IsSearching;

    [RelayCommand(CanExecute = nameof(CanRequest))]
    private async Task RequestDriverAsync()
    {
        try
        {
            IsSearching = true;
            RequestDriverCommand.NotifyCanExecuteChanged();
            await OrderService.RequestDeliveryDriverAsync(OrderId);
            CanRequestDriver = false;
            IsWaitingForDriver = true;
        }
        catch (Exception ex)
        {
            await Dialogs.AlertAsync("Couldn't request a driver", ex.Message);
        }
        finally
        {
            IsSearching = false;
            RequestDriverCommand.NotifyCanExecuteChanged();
        }
    }

    [RelayCommand]
    private Task BackToOrdersAsync() => Navigation.GoToAsync(Routes.ProviderOrders, Param(Routes.FilterKey, nameof(ProviderOrderFilter.Ready)));
}
