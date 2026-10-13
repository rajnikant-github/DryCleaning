using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using DryCleaning.Helpers;
using DryCleaning.Models;
using DryCleaning.Services;
using DryCleaning.ViewModels.Items;

namespace DryCleaning.ViewModels.Customer;

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
