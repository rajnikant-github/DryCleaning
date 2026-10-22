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
    public ObservableCollection<ExtraChargeLine> ExtraCharges { get; } = [];
    public ObservableCollection<TimelineStep> Timeline { get; } = [];

    /// <summary>The five tappable stars of the "Rate your experience" card.</summary>
    public ObservableCollection<StarItem> RatingStars { get; } = [.. Enumerable.Range(1, 5).Select(n => new StarItem(n))];

    /// <summary>The five read-only stars of a rating that was already given.</summary>
    public ObservableCollection<StarItem> GivenStars { get; } = [.. Enumerable.Range(1, 5).Select(n => new StarItem(n))];

    [ObservableProperty] private string createdText = string.Empty;
    [ObservableProperty] private string statusMessage = string.Empty;
    [ObservableProperty] private string addressText = string.Empty;
    [ObservableProperty] private decimal subTotal;
    [ObservableProperty] private decimal deliveryFee;
    [ObservableProperty] private decimal tax;
    [ObservableProperty] private decimal total;
    [ObservableProperty] private string vatLabel = "VAT";
    [ObservableProperty] private bool hasVat;
    [ObservableProperty] private string itemCountText = string.Empty;

    [ObservableProperty] private string paymentText = string.Empty;
    [ObservableProperty] private bool isPaid;
    [ObservableProperty] private bool canPay;
    [ObservableProperty] private bool canCancel;
    [ObservableProperty] private bool canRate;
    [ObservableProperty] private bool hasRating;
    [ObservableProperty] private int selectedStars;
    [ObservableProperty] private string ratingComment = string.Empty;
    [ObservableProperty] private string givenRatingText = string.Empty;
    [ObservableProperty] private string givenRatingComment = string.Empty;
    [ObservableProperty] private bool showDriverNotes;
    [ObservableProperty] private bool canEditDriverNotes;
    [ObservableProperty] private string driverNotes = string.Empty;
    [ObservableProperty] private string driverNotesText = string.Empty;
    [ObservableProperty] private string driverNotesButtonText = "Add";
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
        VatLabel = VatText.Label(order.VatRate);
        HasVat = order.VatRate > 0 || order.Tax > 0;
        ItemCountText = d.ExpectedItemCount == 1 ? "1 item" : $"{d.ExpectedItemCount} items";

        Items.Clear();
        ExtraCharges.Clear();
        foreach (var charge in d.ExtraCharges)
            ExtraCharges.Add(charge);
        foreach (var item in d.Items)
            Items.Add(item);

        IsPaid = d.IsPaid;
        PaymentText = d.Payment is null
            ? "Not paid yet"
            : $"{d.Payment.Method} · {d.Payment.Status}{(d.Payment.PaidAt is { } at ? $" · {at:d MMM, HH:mm}" : "")}";

        CanPay = order.Status == OrderStatus.Created && !d.IsPaid;
        CanCancel = order.Status is OrderStatus.Created or OrderStatus.Confirmed or OrderStatus.PickupAssigned;
        // The note can be added or changed until the driver has collected the items.
        CanEditDriverNotes = CanCancel;
        DriverNotes = order.DriverNotes ?? string.Empty;
        DriverNotesText = string.IsNullOrWhiteSpace(DriverNotes) ? "No note added" : DriverNotes;
        DriverNotesButtonText = string.IsNullOrWhiteSpace(DriverNotes) ? "Add" : "Edit";
        ShowDriverNotes = CanEditDriverNotes || !string.IsNullOrWhiteSpace(DriverNotes);
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

        CanRate = d.CanRate;
        HasRating = d.Rating is not null;
        GivenRatingText = d.Rating is null ? string.Empty : $"You rated {d.ProviderName} {d.Rating.Stars} out of 5";
        GivenRatingComment = d.Rating?.Comment ?? string.Empty;
        foreach (var star in GivenStars)
            star.IsFilled = d.Rating is not null && star.Number <= d.Rating.Stars;
        if (!d.CanRate)
        {
            SelectedStars = 0;
            RatingComment = string.Empty;
            foreach (var star in RatingStars)
                star.IsFilled = false;
        }

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
    private void SetStars(int stars)
    {
        SelectedStars = stars;
        foreach (var star in RatingStars)
            star.IsFilled = star.Number <= stars;
    }

    [RelayCommand]
    private Task SubmitRatingAsync() => RunBusyAsync(async () =>
    {
        if (SelectedStars is < 1 or > 5)
        {
            await Dialogs.AlertAsync("Choose a rating", "Tap the stars to rate the provider from 1 to 5.");
            return;
        }

        await Orders.RateOrderAsync(OrderId, SelectedStars, RatingComment);
        await LoadAsync(showSkeleton: false);
    });

    [RelayCommand]
    private Task EditDriverNotesAsync() => RunBusyAsync(async () =>
    {
        var notes = await Dialogs.PromptAsync("Note for the driver",
            "For example: leave the parcel at the gate, or call when you arrive. Leave empty to remove the note.",
            "Save", "Add a note for the driver", DriverNotes);
        if (notes is null)
            return;

        await Orders.UpdateDriverNotesAsync(OrderId, notes.Trim());
        await LoadAsync(showSkeleton: false);
    });

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

        var description = await Dialogs.PromptAsync(reason, "Tell us what happened. E Dry Cleaners support will review it with the provider.", "Submit");
        if (string.IsNullOrWhiteSpace(description))
            return;

        await Orders.RaiseDisputeAsync(OrderId, reason, description.Trim());
        await Dialogs.AlertAsync("Dispute raised", "Thanks – our support team will contact you shortly.");
        await LoadAsync(showSkeleton: false);
    });
}
