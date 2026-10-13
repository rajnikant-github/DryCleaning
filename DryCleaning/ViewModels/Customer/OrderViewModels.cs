using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using DryCleaning.Helpers;
using DryCleaning.Models;
using DryCleaning.Services;
using DryCleaning.ViewModels.Items;

namespace DryCleaning.ViewModels.Customer;

/// <summary>Shared helpers for customer pages that show one order.</summary>
public abstract partial class CustomerOrderViewModelBase(ViewModelServices services, ICustomerOrderService orders)
    : BaseViewModel(services), IQueryAttributable
{
    protected ICustomerOrderService Orders { get; } = orders;
    protected Guid OrderId { get; private set; }

    [ObservableProperty] private string orderNumber = string.Empty;
    [ObservableProperty] private OrderStatus status;
    [ObservableProperty] private string providerName = string.Empty;
    [ObservableProperty] private string providerPhone = string.Empty;
    [ObservableProperty] private string pickupText = string.Empty;
    [ObservableProperty] private string estimatedCompletionText = string.Empty;

    public void ApplyQueryAttributes(IDictionary<string, object> query)
    {
        if (TryGetGuid(query, Routes.OrderIdKey, out var id))
            OrderId = id;
    }

    protected override async Task<bool> LoadDataAsync()
    {
        var detail = await Orders.GetOrderAsync(OrderId);
        OrderNumber = detail.Order.OrderNumber;
        Status = detail.Order.Status;
        ProviderName = detail.ProviderName;
        ProviderPhone = detail.ProviderPhone;
        PickupText = FormatPickup(detail.Order);
        EstimatedCompletionText = EstimateCompletion(detail);
        OnDetailLoaded(detail);
        return true;
    }

    protected abstract void OnDetailLoaded(OrderDetail detail);

    protected static string FormatPickup(Order order) => order.PickupDate is { } date
        ? $"{date:ddd, d MMM}{(string.IsNullOrEmpty(order.PickupSlot) ? "" : $" · {order.PickupSlot}")}"
        : "To be scheduled";

    protected static string EstimateCompletion(OrderDetail detail)
    {
        if (detail.Order.PickupDate is not { } pickup)
            return string.Empty;
        var days = detail.Items.Count == 0 ? 2 : detail.Items.Max(i => i.EstimatedDays);
        return pickup.AddDays(days + 1).ToString("ddd, d MMM");
    }

    protected async Task CallAsync(string title, string? phone)
    {
        if (string.IsNullOrWhiteSpace(phone))
            return;
        try
        {
            if (PhoneDialer.Default.IsSupported)
                PhoneDialer.Default.Open(phone);
            else
                await Dialogs.AlertAsync(title, phone);
        }
        catch (Exception)
        {
            await Dialogs.AlertAsync(title, phone);
        }
    }

    [RelayCommand]
    private Task CallProviderAsync() => CallAsync($"Call {ProviderName}", ProviderPhone);
}

/// <summary>C15 – Order confirmed.</summary>
public sealed partial class OrderConfirmedViewModel(ViewModelServices services, ICustomerOrderService orders)
    : CustomerOrderViewModelBase(services, orders)
{
    [ObservableProperty] private decimal total;
    [ObservableProperty] private string itemCountText = string.Empty;
    [ObservableProperty] private string addressText = string.Empty;

    protected override void OnDetailLoaded(OrderDetail detail)
    {
        Total = detail.Order.TotalAmount;
        ItemCountText = detail.ExpectedItemCount == 1 ? "1 item" : $"{detail.ExpectedItemCount} items";
        AddressText = detail.PickupAddress.FormattedAddress;
    }

    [RelayCommand]
    private Task TrackOrderAsync() =>
        Navigation.GoToAsync($"../{Routes.OrderTracking}", Param(Routes.OrderIdKey, OrderId));

    [RelayCommand]
    private Task ViewOrderAsync() =>
        Navigation.GoToAsync($"../{Routes.CustomerOrderDetail}", Param(Routes.OrderIdKey, OrderId));

    [RelayCommand]
    private Task GoHomeAsync() => Navigation.GoToAsync(Routes.CustomerHome);
}

/// <summary>C16 – Live order tracking.</summary>
public sealed partial class OrderTrackingViewModel(ViewModelServices services, ICustomerOrderService orders)
    : CustomerOrderViewModelBase(services, orders)
{
    public ObservableCollection<TimelineStep> Timeline { get; } = [];

    [ObservableProperty] private string statusMessage = string.Empty;
    [ObservableProperty] private bool showMap;
    [ObservableProperty] private string mapCaption = string.Empty;

    [ObservableProperty] private bool hasDriver;
    [ObservableProperty] private string driverName = string.Empty;
    [ObservableProperty] private string driverInitials = string.Empty;
    [ObservableProperty] private string driverInfo = string.Empty;
    [ObservableProperty] private string driverPhone = string.Empty;

    [ObservableProperty] private bool showPickupCode;
    [ObservableProperty] private string pickupCode = string.Empty;
    [ObservableProperty] private bool showDeliveryOtp;
    [ObservableProperty] private string deliveryOtp = string.Empty;

    [ObservableProperty] private bool needsPayment;

    protected override void OnDetailLoaded(OrderDetail d)
    {
        Title = d.Order.OrderNumber;
        StatusMessage = d.Order.Status.CustomerStatusMessage(d.IsPaid);
        NeedsPayment = d.Order.Status == OrderStatus.Created && !d.IsPaid;

        // Which driver is relevant right now?
        var driver = d.Order.Status switch
        {
            OrderStatus.PickupAssigned or OrderStatus.PickedUp => d.PickupDriver,
            OrderStatus.DeliveryAssigned or OrderStatus.OutForDelivery => d.DeliveryDriver,
            _ => null
        };
        HasDriver = driver is not null;
        DriverName = driver?.DriverName ?? string.Empty;
        DriverInitials = StringHelpers.Initials(driver?.DriverName);
        DriverPhone = driver?.DriverPhone ?? string.Empty;
        DriverInfo = driver is null ? string.Empty
            : driver.JobType == DriverJobType.Pickup ? "Collecting your items" : "Delivering your order";

        ShowMap = d.Order.Status is OrderStatus.PickupAssigned or OrderStatus.PickedUp or OrderStatus.OutForDelivery;
        MapCaption = d.Order.Status switch
        {
            OrderStatus.PickupAssigned => "Driver on the way to you",
            OrderStatus.PickedUp => $"On the way to {d.ProviderName}",
            OrderStatus.OutForDelivery => "Driver on the way to you",
            _ => string.Empty
        };

        ShowPickupCode = !string.IsNullOrEmpty(d.PickupCode) &&
                         d.Order.Status is OrderStatus.Created or OrderStatus.Confirmed or OrderStatus.PickupAssigned;
        PickupCode = d.PickupCode ?? string.Empty;
        ShowDeliveryOtp = !string.IsNullOrEmpty(d.DeliveryOtp) &&
                          d.Order.Status is OrderStatus.DeliveryAssigned or OrderStatus.OutForDelivery;
        DeliveryOtp = d.DeliveryOtp ?? string.Empty;

        Timeline.Clear();
        foreach (var step in TimelineBuilder.Build(d, s => s.CustomerTimelineTitle()))
            Timeline.Add(step);
    }

    [RelayCommand]
    private Task CallDriverAsync() => CallAsync($"Call {DriverName}", DriverPhone);

    [RelayCommand]
    private Task ViewDetailsAsync() =>
        Navigation.GoToAsync(Routes.CustomerOrderDetail, Param(Routes.OrderIdKey, OrderId));

    [RelayCommand]
    private Task PayNowAsync() => Navigation.GoToAsync(Routes.Payment, Param(Routes.OrderIdKey, OrderId));
}

/// <summary>C17 – My orders (Active / Completed / Cancelled).</summary>
public sealed partial class CustomerOrdersViewModel(ViewModelServices services, ICustomerOrderService orders)
    : BaseViewModel(services)
{
    private CustomerOrderFilter _filter = CustomerOrderFilter.Active;

    public ObservableCollection<SegmentItem> Segments { get; } = [];
    public ObservableCollection<OrderCardItem> Orders { get; } = [];

    [ObservableProperty] private string emptyText = "No active orders";

    protected override async Task<bool> LoadDataAsync()
    {
        if (Segments.Count == 0)
        {
            foreach (var filter in Enum.GetValues<CustomerOrderFilter>())
                Segments.Add(new SegmentItem(filter.ToString(), filter.ToString(), OnSegmentSelected) { IsSelected = filter == _filter });
        }

        var counts = await orders.GetOrderCountsAsync();
        foreach (var segment in Segments)
            segment.Count = counts.TryGetValue(Enum.Parse<CustomerOrderFilter>(segment.Key), out var c) ? c : 0;

        var list = await orders.GetOrdersAsync(_filter);
        Orders.Clear();
        foreach (var order in list)
            Orders.Add(new OrderCardItem(order, OpenAsync, title: order.ProviderName,
                hint: order.Status.CustomerStatusMessage(order.IsPaid)));

        EmptyText = _filter switch
        {
            CustomerOrderFilter.Active => "No active orders",
            CustomerOrderFilter.Completed => "No completed orders yet",
            _ => "No cancelled orders"
        };
        return Orders.Count > 0;
    }

    private void OnSegmentSelected(SegmentItem segment)
    {
        foreach (var s in Segments)
            s.IsSelected = ReferenceEquals(s, segment);
        _filter = Enum.Parse<CustomerOrderFilter>(segment.Key);
        _ = LoadAsync(showSkeleton: true);
    }

    private Task OpenAsync(OrderSummary order) =>
        Navigation.GoToAsync(CustomerFlow.IsActive(order.Status) ? Routes.OrderTracking : Routes.CustomerOrderDetail,
            Param(Routes.OrderIdKey, order.Id));

    [RelayCommand]
    private Task NewOrderAsync() => Navigation.GoToAsync(Routes.Categories);
}

/// <summary>C18 – Order detail (items, payment, codes, cancel / dispute).</summary>
public sealed partial class CustomerOrderDetailViewModel(ViewModelServices services, ICustomerOrderService orders)
    : CustomerOrderViewModelBase(services, orders)
{
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
