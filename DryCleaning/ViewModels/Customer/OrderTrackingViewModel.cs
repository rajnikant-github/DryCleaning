using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using DryCleaning.Helpers;
using DryCleaning.Models;
using DryCleaning.Services;
using DryCleaning.ViewModels.Items;

namespace DryCleaning.ViewModels.Customer;

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
