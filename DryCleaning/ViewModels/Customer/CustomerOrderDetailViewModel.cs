using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using DryCleaning.Helpers;
using DryCleaning.Models;
using DryCleaning.Services;
using DryCleaning.Services.Realtime;
using DryCleaning.ViewModels.Items;

namespace DryCleaning.ViewModels.Customer;

/// <summary>C18 – Order detail (items, payment, codes, cancel / dispute).</summary>
public sealed partial class CustomerOrderDetailViewModel(ViewModelServices services, ICustomerOrderService orders)
    : CustomerOrderViewModelBase(services, orders)
{
    protected override bool ReactsTo(RealtimeChangedMessage message) =>
        message.Kind == RealtimeChangeKind.Reconnected || (message.Kind == RealtimeChangeKind.OrderUpdated && message.OrderId == OrderId);

    public ObservableCollection<OrderItemDetail> Items { get; } = [];
    public ObservableCollection<TimelineStep> Timeline { get; } = [];

    [ObservableProperty] private string createdText = string.Empty;
    [ObservableProperty] private string statusMessage = string.Empty;
    [ObservableProperty] private string addressText = string.Empty;
    [ObservableProperty] private decimal subTotal;
    [ObservableProperty] private decimal deliveryFee;
    [ObservableProperty] private decimal tax;
    [ObservableProperty] private decimal total;
    [ObservableProperty] private string vatLabel = "VAT";
    [ObservableProperty] private string itemCountText = string.Empty;

    [ObservableProperty] private string paymentText = string.Empty;
    [ObservableProperty] private bool isPaid;
    [ObservableProperty] private bool canPay;
    [ObservableProperty] private bool canCancel;
    [ObservableProperty] private bool canRaiseDispute;
    [ObservableProperty] private bool isActive;

    [ObservableProperty] private bool showPickupCode;
    [ObservableProperty] private string pickupCode = string.Empty;
    [ObservableProperty] private bool showDeliveryOtp;
    [ObservableProperty] private string deliveryOtp = string.Empty;

    [ObservableProperty] private bool hasDispute;
    [ObservableProperty] private string disputeTitle = string.Empty;
    [ObservableProperty] private string disputeText = string.Empty;

    public bool HasFooter => CanPay || CanCancel || CanRaiseDispute || IsActive;

    protected override void OnDetailLoaded(OrderDetail d)
    {
        var order = d.Order;
        Title = order.OrderNumber;
        CreatedText = $"Placed {order.CreatedAt:d MMM yyyy, HH:mm}";
        StatusMessage = order.Status.CustomerStatusMessage(d.IsPaid);
        AddressText = d.PickupAddress.FormattedAddress;
        SubTotal = order.SubTotal;
        DeliveryFee = order.DeliveryFee;
        Tax = order.Tax;
        Total = order.TotalAmount;
        VatLabel = $"VAT ({Services.Settings.Pricing.VatRate:P0})";
        ItemCountText = d.ExpectedItemCount == 1 ? "1 item" : $"{d.ExpectedItemCount} items";

        Items.Clear();
        foreach (var item in d.Items)
            Items.Add(item);

        IsPaid = d.IsPaid;
        PaymentText = d.Payment is null
            ? "Not paid yet"
            : $"{d.Payment.Method} · {d.Payment.Status}{(d.Payment.PaidAt is { } at ? $" · {at:d MMM, HH:mm}" : "")}";

        CanPay = order.Status == OrderStatus.Created && !d.IsPaid;
        CanCancel = order.Status is OrderStatus.Created or OrderStatus.Confirmed or OrderStatus.PickupAssigned;
        CanRaiseDispute = d.OpenDispute is null &&
                          order.Status != OrderStatus.Created &&
                          OrderWorkflow.CanTransition(order.Status, OrderStatus.Disputed);
        IsActive = CustomerFlow.IsActive(order.Status) && order.Status != OrderStatus.Disputed;

        ShowPickupCode = !string.IsNullOrEmpty(d.PickupCode) &&
                         order.Status is OrderStatus.Created or OrderStatus.Confirmed or OrderStatus.PickupAssigned;
        PickupCode = d.PickupCode ?? string.Empty;
        ShowDeliveryOtp = !string.IsNullOrEmpty(d.DeliveryOtp) &&
                          order.Status is OrderStatus.DeliveryAssigned or OrderStatus.OutForDelivery;
        DeliveryOtp = d.DeliveryOtp ?? string.Empty;

        HasDispute = d.OpenDispute is not null;
        DisputeTitle = d.OpenDispute is null ? string.Empty : $"{d.OpenDispute.Reason} · {d.OpenDispute.Status}";
        DisputeText = d.OpenDispute?.Description ?? string.Empty;

        Timeline.Clear();
        foreach (var step in TimelineBuilder.Build(d, s => s.CustomerTimelineTitle()))
            Timeline.Add(step);

        OnPropertyChanged(nameof(HasFooter));
    }

    [RelayCommand]
    private Task PayNowAsync() => Navigation.GoToAsync(Routes.Payment, Param(Routes.OrderIdKey, OrderId));

    [RelayCommand]
    private Task TrackAsync() => Navigation.GoToAsync(Routes.OrderTracking, Param(Routes.OrderIdKey, OrderId));

    [RelayCommand]
    private Task CancelOrderAsync() => RunBusyAsync(async () =>
    {
        var reason = await Dialogs.ActionSheetAsync($"Cancel {OrderNumber}?", "Keep order",
            "Changed my mind", "Wrong items or address", "Pickup time doesn't suit me", "Other");
        if (reason is null)
            return;

        var refundNote = IsPaid ? " Your payment will be refunded to the original payment method." : string.Empty;
        if (!await Dialogs.ConfirmAsync("Cancel order", $"Are you sure you want to cancel this order?{refundNote}", "Cancel order", "Keep order"))
            return;

        await Orders.CancelOrderAsync(OrderId, reason);
        await LoadAsync(showSkeleton: false);
    });

    [RelayCommand]
    private Task RaiseDisputeAsync() => RunBusyAsync(async () =>
    {
        var reason = await Dialogs.ActionSheetAsync("Report a problem", "Cancel",
            "Item damaged", "Item missing", "Not cleaned properly", "Late delivery", "Other");
        if (reason is null)
            return;

        var description = await Dialogs.PromptAsync(reason, "Tell us what happened. EDry Cleaners support will review it with the provider.", "Submit");
        if (string.IsNullOrWhiteSpace(description))
            return;

        await Orders.RaiseDisputeAsync(OrderId, reason, description.Trim());
        await Dialogs.AlertAsync("Dispute raised", "Thanks – our support team will contact you shortly.");
        await LoadAsync(showSkeleton: false);
    });
}
