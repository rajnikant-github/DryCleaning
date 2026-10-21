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
    public ObservableCollection<ExtraChargeLine> ExtraCharges { get; } = [];

    [ObservableProperty] private string providerName = string.Empty;
    [ObservableProperty] private string addressText = string.Empty;
    [ObservableProperty] private string pickupText = string.Empty;
    [ObservableProperty] private string estimatedCompletionText = string.Empty;
    [ObservableProperty] private decimal subTotal;
    [ObservableProperty] private decimal deliveryFee;
    [ObservableProperty] private decimal tax;
    [ObservableProperty] private decimal total;
    [ObservableProperty] private string deliveryLabel = "Pickup & delivery";
    [ObservableProperty] private string deliveryText = string.Empty;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(VatLabel))]
    [NotifyPropertyChangedFor(nameof(HasVat))]
    private decimal vatRate;

    public string VatLabel => VatText.Label(VatRate);
    public bool HasVat => VatRate > 0;

    protected override async Task<bool> LoadDataAsync()
    {
        ProviderName = cart.ProviderName;
        AddressText = cart.PickupAddress?.FormattedAddress ?? "No address selected";
        PickupText = cart.PickupDate is { } date ? $"{date:dddd, d MMMM} · {cart.PickupSlot}" : "No time selected";
        EstimatedCompletionText = cart.PickupDate is { } pickup
            ? $"Estimated delivery by {pickup.AddDays(cart.EstimatedDays + 1):ddd, d MMM}"
            : string.Empty;

        Lines.Clear();
        ExtraCharges.Clear();
        foreach (var line in cart.Lines)
            Lines.Add(line);
        // Until the server has quoted the price, delivery is left out rather than guessed.
        SubTotal = cart.SubTotal;
        DeliveryFee = 0;
        Tax = 0;
        VatRate = 0;
        Total = cart.SubTotal;
        DeliveryLabel = "Pickup & delivery";
        DeliveryText = "Calculated when you pay";

        // The server decides the delivery fee from the real distance, so show its price (what will be charged), not a local guess.
        try
        {
            if (await orders.QuoteAsync(cart) is { } quote)
            {
                SubTotal = quote.SubTotal;
                DeliveryFee = quote.DeliveryFee;
                Tax = quote.Tax;
                VatRate = quote.VatRate;
                Total = quote.Total;
                DeliveryText = CurrencyFormatter.Current.Format(quote.DeliveryFee);
                ExtraCharges.Clear();
                foreach (var charge in quote.ExtraCharges)
                    ExtraCharges.Add(charge);
                if (quote.DistanceKm is { } km)
                    DeliveryLabel = $"Pickup & delivery · {km.ToString("0.#", System.Globalization.CultureInfo.InvariantCulture)} km{(quote.DistanceIsEstimate ? " (est.)" : "")}";
            }
        }
        catch (Exception ex)
        {
            // No quote (offline, server busy): delivery stays "calculated when you pay"; the server prices the order when it is placed.
            System.Diagnostics.Debug.WriteLine($"[OrderSummary] Quote failed: {ex.Message}");
        }

        return !cart.IsEmpty;
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
