using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using DryCleaning.Helpers;
using DryCleaning.Models;
using DryCleaning.Services;
using DryCleaning.ViewModels.Items;

namespace DryCleaning.ViewModels.Provider;

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
