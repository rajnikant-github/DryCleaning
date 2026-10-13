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
