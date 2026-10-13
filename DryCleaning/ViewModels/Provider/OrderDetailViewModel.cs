using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using DryCleaning.Helpers;
using DryCleaning.Models;
using DryCleaning.Services;
using DryCleaning.ViewModels.Items;

namespace DryCleaning.ViewModels.Provider;

/// <summary>P04 – Order details with the next contextual action.</summary>
public sealed partial class OrderDetailViewModel(ViewModelServices services, IProviderOrderService orderService)
    : OrderPageViewModelBase(services, orderService)
{
    public ObservableCollection<TimelineStep> Timeline { get; } = [];
    public ObservableCollection<string> PhotoPaths { get; } = [];

    [ObservableProperty] private string createdText = string.Empty;
    [ObservableProperty] private string customerPhone = string.Empty;
    [ObservableProperty] private string customerInitials = string.Empty;
    [ObservableProperty] private string pickupAddress = string.Empty;
    [ObservableProperty] private decimal subTotal;
    [ObservableProperty] private decimal tax;
    [ObservableProperty] private decimal deliveryFee;
    [ObservableProperty] private decimal totalAmount;
    [ObservableProperty] private string itemCountText = string.Empty;
    [ObservableProperty] private string vatLabel = "VAT";

    [ObservableProperty] private bool hasPickupDriver;
    [ObservableProperty] private string pickupDriverName = string.Empty;
    [ObservableProperty] private string pickupDriverInfo = string.Empty;
    [ObservableProperty] private bool hasDeliveryDriver;
    [ObservableProperty] private string deliveryDriverName = string.Empty;
    [ObservableProperty] private string deliveryDriverInfo = string.Empty;

    [ObservableProperty] private bool hasPayment;
    [ObservableProperty] private string paymentText = string.Empty;

    [ObservableProperty] private bool hasDispute;
    [ObservableProperty] private string disputeTitle = string.Empty;
    [ObservableProperty] private string disputeText = string.Empty;

    [ObservableProperty] private bool hasPhotos;

    [ObservableProperty] private bool showAcceptReject;
    [ObservableProperty] private bool hasPrimaryAction;
    [ObservableProperty] private string primaryActionText = string.Empty;
    [ObservableProperty] private bool hasWaitingHint;
    [ObservableProperty] private string waitingHint = string.Empty;
    [ObservableProperty] private bool showDemoPickup;
    [ObservableProperty] private bool canReportIssue;

    public bool HasFooter => ShowAcceptReject || HasPrimaryAction || HasWaitingHint || CanReportIssue;

    protected override void OnDetailLoaded(OrderDetail d)
    {
        var order = d.Order;
        CreatedText = $"Placed {order.CreatedAt:d MMM yyyy, HH:mm}";
        CustomerPhone = d.CustomerPhone;
        CustomerInitials = StringHelpers.Initials(d.CustomerName);
        PickupAddress = d.PickupAddress.FormattedAddress;
        SubTotal = order.SubTotal;
        Tax = order.Tax;
        DeliveryFee = order.DeliveryFee;
        TotalAmount = order.TotalAmount;
        ItemCountText = d.ExpectedItemCount == 1 ? "1 item" : $"{d.ExpectedItemCount} items";
        VatLabel = $"VAT ({Services.Settings.Pricing.VatRate:P0})";

        HasPickupDriver = d.PickupDriver is not null;
        PickupDriverName = d.PickupDriver?.DriverName ?? string.Empty;
        PickupDriverInfo = d.PickupDriver is null ? string.Empty : $"{d.PickupDriver.LicenseNumber} · {d.PickupDriver.Status}";
        HasDeliveryDriver = d.DeliveryDriver is not null;
        DeliveryDriverName = d.DeliveryDriver?.DriverName ?? string.Empty;
        DeliveryDriverInfo = d.DeliveryDriver is null ? string.Empty : $"{d.DeliveryDriver.LicenseNumber} · {d.DeliveryDriver.Status}";

        HasPayment = d.Payment is not null;
        PaymentText = d.Payment is null ? string.Empty
            : $"{d.Payment.Method} · {d.Payment.Status}{(d.Payment.TransactionId is null ? "" : $" · {d.Payment.TransactionId}")}";

        HasDispute = d.OpenDispute is not null;
        DisputeTitle = d.OpenDispute is null ? string.Empty : $"{d.OpenDispute.Reason} · {d.OpenDispute.Status}";
        DisputeText = d.OpenDispute?.Description ?? string.Empty;

        PhotoPaths.Clear();
        foreach (var photo in d.Photos.Where(p => p.Url.StartsWith("http", StringComparison.OrdinalIgnoreCase) || File.Exists(p.Url)))
            PhotoPaths.Add(photo.Url);
        HasPhotos = PhotoPaths.Count > 0;

        BuildTimeline(d);
        BuildActions(order.Status, d);
    }

    private void BuildTimeline(OrderDetail d)
    {
        Timeline.Clear();
        foreach (var step in TimelineBuilder.Build(d, status => status.TimelineTitle()))
            Timeline.Add(step);
    }

    private void BuildActions(OrderStatus status, OrderDetail d)
    {
        ShowAcceptReject = status == OrderStatus.Created;

        var action = ProviderActions.ForStatus(status);
        var waitingForDeliveryDriver = d.AwaitingDriverFor == DriverJobType.Delivery;
        if (waitingForDeliveryDriver)
            action = null;
        HasPrimaryAction = action is not null;
        PrimaryActionText = action?.Label ?? string.Empty;

        WaitingHint = waitingForDeliveryDriver
            ? "Delivery job posted · waiting for a driver to accept"
            : ProviderActions.HintFor(status) ?? string.Empty;
        HasWaitingHint = !HasPrimaryAction && !string.IsNullOrEmpty(WaitingHint);

        ShowDemoPickup = Services.Settings.Features.ShowDemoActions &&
                         status is OrderStatus.Confirmed or OrderStatus.PickupAssigned;

        CanReportIssue = d.OpenDispute is null &&
                         OrderWorkflow.CanTransition(status, OrderStatus.Disputed) &&
                         status is not (OrderStatus.Confirmed or OrderStatus.PickupAssigned);

        OnPropertyChanged(nameof(HasFooter));
    }

    [RelayCommand]
    private Task AcceptAsync() => RunBusyAsync(async () =>
    {
        await OrderService.AcceptOrderAsync(OrderId);
        await Dialogs.AlertAsync("Order accepted", "A marketplace driver has been assigned to collect the items from the customer.");
        await LoadAsync(showSkeleton: false);
    });

    [RelayCommand]
    private Task RejectAsync() => RunBusyAsync(async () =>
    {
        var reason = await Dialogs.ActionSheetAsync($"Reject {OrderNumber}?", "Cancel",
            "Fully booked today", "Service not available", "Outside service area", "Other");
        if (reason is null)
            return;

        if (reason == "Other")
        {
            reason = await Dialogs.PromptAsync("Reason for rejection", "Let the customer know why.", "Reject order");
            if (string.IsNullOrWhiteSpace(reason))
                return;
        }

        await OrderService.RejectOrderAsync(OrderId, reason);
        await LoadAsync(showSkeleton: false);
    });

    [RelayCommand]
    private Task PrimaryActionAsync()
    {
        var action = ProviderActions.ForStatus(Status);
        return action is null
            ? Task.CompletedTask
            : Navigation.GoToAsync(action.Route, Param(Routes.OrderIdKey, OrderId));
    }

    [RelayCommand]
    private Task SimulateDriverPickupAsync() => RunBusyAsync(async () =>
    {
        await OrderService.SimulateDriverPickupAsync(OrderId);
        await LoadAsync(showSkeleton: false);
    });

    [RelayCommand]
    private Task ReportIssueAsync() => RunBusyAsync(async () =>
    {
        var reason = await Dialogs.ActionSheetAsync("Report an issue", "Cancel",
            "Items missing", "Item damaged", "Customer unreachable", "Other");
        if (reason is null)
            return;

        var description = await Dialogs.PromptAsync(reason, "Describe the issue. The order will be put on hold until support resolves it.", "Raise dispute");
        if (string.IsNullOrWhiteSpace(description))
            return;

        await OrderService.RaiseDisputeAsync(OrderId, reason, description.Trim());
        await LoadAsync(showSkeleton: false);
    });

    [RelayCommand]
    private async Task CallCustomerAsync()
    {
        try
        {
            if (PhoneDialer.Default.IsSupported && !string.IsNullOrWhiteSpace(CustomerPhone))
                PhoneDialer.Default.Open(CustomerPhone);
            else
                await Dialogs.AlertAsync("Call customer", CustomerPhone);
        }
        catch (Exception)
        {
            await Dialogs.AlertAsync("Call customer", CustomerPhone);
        }
    }
}
