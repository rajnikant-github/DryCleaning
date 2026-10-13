using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using DryCleaning.Helpers;
using DryCleaning.Models;
using DryCleaning.Services;
using DryCleaning.ViewModels.Items;

namespace DryCleaning.ViewModels.Customer;

/// <summary>C13 – Order summary before payment.</summary>
public sealed partial class OrderSummaryViewModel(ViewModelServices services, ICartService cart, ICustomerOrderService orders)
    : BaseViewModel(services)
{
    private Guid? _placedOrderId;
    private string _placedSignature = string.Empty;

    public ObservableCollection<CartLine> Lines { get; } = [];

    [ObservableProperty] private string providerName = string.Empty;
    [ObservableProperty] private string addressText = string.Empty;
    [ObservableProperty] private string pickupText = string.Empty;
    [ObservableProperty] private string estimatedCompletionText = string.Empty;
    [ObservableProperty] private decimal subTotal;
    [ObservableProperty] private decimal deliveryFee;
    [ObservableProperty] private decimal tax;
    [ObservableProperty] private decimal total;

    public string VatLabel => $"VAT ({Services.Settings.Pricing.VatRate:P0})";

    protected override Task<bool> LoadDataAsync()
    {
        ProviderName = cart.ProviderName;
        AddressText = cart.PickupAddress?.FormattedAddress ?? "No address selected";
        PickupText = cart.PickupDate is { } date ? $"{date:dddd, d MMMM} · {cart.PickupSlot}" : "No time selected";
        EstimatedCompletionText = cart.PickupDate is { } pickup
            ? $"Estimated delivery by {pickup.AddDays(cart.EstimatedDays + 1):ddd, d MMM}"
            : string.Empty;

        Lines.Clear();
        foreach (var line in cart.Lines)
            Lines.Add(line);
        SubTotal = cart.SubTotal;
        DeliveryFee = cart.DeliveryFee;
        Tax = cart.Tax;
        Total = cart.Total;
        return Task.FromResult(!cart.IsEmpty);
    }

    [RelayCommand]
    private Task EditAddressAsync() => Navigation.GoToAsync(Routes.PickupAddress, Param(Routes.ModeKey, "edit"));

    [RelayCommand]
    private Task EditTimeAsync() => Navigation.GoToAsync(Routes.PickupTime, Param(Routes.ModeKey, "edit"));

    [RelayCommand]
    private Task ProceedToPaymentAsync() => RunBusyAsync(async () =>
    {
        // Re-use the order if the customer came back from the payment screen without changing anything.
        var signature = string.Join("|", cart.Lines.Select(l => $"{l.ServiceItemId}:{l.Quantity}:{l.Notes}"))
                        + $"|{cart.PickupAddress?.Id}|{cart.PickupDate:yyyyMMdd}|{cart.PickupSlot}";
        if (_placedOrderId is null || signature != _placedSignature)
        {
            _placedOrderId = await orders.PlaceOrderAsync(cart);
            _placedSignature = signature;
        }

        await Navigation.GoToAsync(Routes.Payment, Param(Routes.OrderIdKey, _placedOrderId.Value));
    });
}
