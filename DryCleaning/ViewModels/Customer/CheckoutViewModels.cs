using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using DryCleaning.Helpers;
using DryCleaning.Models;
using DryCleaning.Services;
using DryCleaning.ViewModels.Items;

namespace DryCleaning.ViewModels.Customer;

/// <summary>C10 – Cart / selected items.</summary>
public sealed partial class CartViewModel : BaseViewModel
{
    private readonly ICartService _cart;

    public CartViewModel(ViewModelServices services, ICartService cart) : base(services)
    {
        _cart = cart;
        _cart.Changed += (_, _) => RefreshTotals();
    }

    public ObservableCollection<CartLineItem> Lines { get; } = [];

    [ObservableProperty] private string providerName = string.Empty;
    [ObservableProperty] private decimal subTotal;
    [ObservableProperty] private decimal deliveryFee;
    [ObservableProperty] private decimal tax;
    [ObservableProperty] private decimal total;
    [ObservableProperty] private string itemCountText = string.Empty;

    public string VatLabel => $"VAT ({Services.Settings.Pricing.VatRate:P0})";

    protected override Task<bool> LoadDataAsync()
    {
        Lines.Clear();
        foreach (var line in _cart.Lines)
            Lines.Add(new CartLineItem(line, _cart, RemoveAsync));
        ProviderName = _cart.ProviderName;
        RefreshTotals();
        return Task.FromResult(!_cart.IsEmpty);
    }

    private void RefreshTotals()
    {
        SubTotal = _cart.SubTotal;
        DeliveryFee = _cart.DeliveryFee;
        Tax = _cart.Tax;
        Total = _cart.Total;
        ItemCountText = _cart.ItemCount == 1 ? "1 item" : $"{_cart.ItemCount} items";

        // Lines removed by setting quantity to 0
        foreach (var removed in Lines.Where(l => _cart.Lines.All(c => c.Id != l.Line.Id)).ToList())
            Lines.Remove(removed);
        if (_cart.IsEmpty && State == ViewState.Content)
            State = ViewState.Empty;
    }

    private async Task RemoveAsync(CartLineItem item)
    {
        if (await Dialogs.ConfirmAsync("Remove item", $"Remove {item.Title} ({item.Line.ServiceItemName}) from your cart?", "Remove", "Cancel"))
            _cart.Remove(item.Line.Id);
    }

    [RelayCommand]
    private Task ContinueAsync() => Navigation.GoToAsync(Routes.PickupAddress, Param(Routes.ModeKey, "checkout"));

    [RelayCommand]
    private Task AddMoreAsync()
    {
        var first = _cart.Lines.FirstOrDefault();
        return first is null || _cart.ProviderId is null
            ? Navigation.GoToAsync(Routes.Categories)
            : Navigation.GoToAsync(Routes.Products, new Dictionary<string, object>
            {
                [Routes.ProviderIdKey] = _cart.ProviderId.Value,
                [Routes.CategoryIdKey] = first.CategoryId
            });
    }

    [RelayCommand]
    private Task BrowseAsync() => Navigation.GoToAsync(Routes.Categories);
}

/// <summary>C11 – Pickup address via Google Places search (one address per role in V1).</summary>
public sealed partial class PickupAddressViewModel(
    ViewModelServices services, IPlacesService places, ICustomerProfileService profiles, ICartService cart)
    : BaseViewModel(services), IQueryAttributable
{
    private string _mode = "checkout";
    private CancellationTokenSource? _searchCts;

    public ObservableCollection<PredictionItem> Predictions { get; } = [];
    public ObservableCollection<KeyValueRow> AddressFields { get; } = [];

    [ObservableProperty] private string searchText = string.Empty;
    [ObservableProperty] private bool isSearching;
    [ObservableProperty] private bool hasPredictions;
    [ObservableProperty] private bool showNoResults;
    [ObservableProperty] private string addressLine2 = string.Empty;
    [ObservableProperty] private bool showDetails;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(HasSelection))]
    [NotifyPropertyChangedFor(nameof(FormattedAddress))]
    [NotifyCanExecuteChangedFor(nameof(ConfirmCommand))]
    private Address? selected;

    public bool HasSelection => Selected is not null;
    public string FormattedAddress => Selected?.FormattedAddress ?? string.Empty;
    public string ConfirmText => _mode == "checkout" ? "Confirm Address" : "Save Address";

    public void ApplyQueryAttributes(IDictionary<string, object> query)
    {
        if (query.TryGetValue(Routes.ModeKey, out var mode) && mode is string m)
            _mode = m;
        OnPropertyChanged(nameof(ConfirmText));
    }

    protected override async Task<bool> LoadDataAsync()
    {
        if (Selected is null)
        {
            var current = cart.PickupAddress ?? (await profiles.GetProfileAsync()).Address;
            if (current is not null)
                SetSelected(current);
        }

        return true;
    }

    partial void OnSearchTextChanged(string value) => _ = SearchAsync(value);

    private async Task SearchAsync(string query)
    {
        _searchCts?.Cancel();
        _searchCts = new CancellationTokenSource();
        var token = _searchCts.Token;

        try
        {
            await Task.Delay(300, token); // debounce typing
            IsSearching = true;
            var results = await places.AutocompleteAsync(query);
            if (token.IsCancellationRequested)
                return;

            Predictions.Clear();
            foreach (var prediction in results)
                Predictions.Add(new PredictionItem(prediction, SelectPredictionAsync));
            HasPredictions = Predictions.Count > 0;
            ShowNoResults = !HasPredictions && query.Trim().Length >= 2;
        }
        catch (TaskCanceledException)
        {
            // newer keystroke
        }
        finally
        {
            if (!token.IsCancellationRequested)
                IsSearching = false;
        }
    }

    private async Task SelectPredictionAsync(PlacePrediction prediction)
    {
        try
        {
            IsBusy = true;
            var address = await places.GetDetailsAsync(prediction.PlaceId);
            SetSelected(address);
            _searchCts?.Cancel();
            SearchText = string.Empty;
            Predictions.Clear();
            HasPredictions = false;
            ShowNoResults = false;
        }
        catch (Exception ex)
        {
            await Dialogs.AlertAsync("Address", ex.Message);
        }
        finally
        {
            IsBusy = false;
        }
    }

    private void SetSelected(Address address)
    {
        Selected = address;
        AddressLine2 = address.AddressLine2 ?? string.Empty;
        AddressFields.Clear();
        void Row(string label, object? value)
        {
            var text = value?.ToString();
            if (!string.IsNullOrWhiteSpace(text))
                AddressFields.Add(new KeyValueRow(label, text));
        }

        Row("Google Place ID", address.GooglePlaceId);
        Row("Address line 1", address.AddressLine1);
        Row("Street number", address.StreetNumber);
        Row("Route", address.Route);
        Row("Sub-locality", address.SubLocality);
        Row("Locality", address.Locality);
        Row("Province", address.AdministrativeAreaLevel1);
        Row("District", address.AdministrativeAreaLevel2);
        Row("Country", address.Country);
        Row("Country code", address.CountryCode);
        Row("Postal code", address.PostalCode);
        Row("Latitude", address.Latitude.ToString("0.0000000"));
        Row("Longitude", address.Longitude.ToString("0.0000000"));
        Row("Plus code", address.PlusCode);
    }

    [RelayCommand]
    private void ToggleDetails() => ShowDetails = !ShowDetails;

    [RelayCommand]
    private Task UseCurrentLocationAsync() => RunBusyAsync(async () =>
    {
        var address = await places.GetCurrentLocationAddressAsync();
        if (address is null)
        {
            await Dialogs.AlertAsync("Location", "Allow location access, or search for your address instead.");
            return;
        }

        SetSelected(address);
    });

    private bool CanConfirm() => Selected is not null;

    [RelayCommand(CanExecute = nameof(CanConfirm))]
    private Task ConfirmAsync() => RunBusyAsync(async () =>
    {
        var address = Selected!;
        address.AddressLine2 = string.IsNullOrWhiteSpace(AddressLine2) ? null : AddressLine2.Trim();
        var saved = await profiles.SaveAddressAsync(address);
        cart.PickupAddress = saved;

        if (_mode == "checkout")
            await Navigation.GoToAsync(Routes.PickupTime);
        else
            await Navigation.GoBackAsync();
    });
}

/// <summary>C12 – Pickup date and time slot.</summary>
public sealed partial class PickupTimeViewModel : BaseViewModel, IQueryAttributable
{
    private readonly ICartService _cart;
    private readonly List<DateTime> _dates = [];
    private string _mode = "checkout";

    public PickupTimeViewModel(ViewModelServices services, ICartService cart) : base(services)
    {
        _cart = cart;
    }

    public ObservableCollection<SegmentItem> Dates { get; } = [];
    public ObservableCollection<SlotItem> Slots { get; } = [];

    [ObservableProperty] private DateTime selectedDate = DateTime.Today;
    [ObservableProperty] private bool hasNoSlots;

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(ContinueCommand))]
    private SlotItem? selectedSlot;

    public string ContinueText => _mode == "edit" ? "Save" : "Continue";

    public void ApplyQueryAttributes(IDictionary<string, object> query)
    {
        if (query.TryGetValue(Routes.ModeKey, out var mode) && mode is string m)
            _mode = m;
        OnPropertyChanged(nameof(ContinueText));
    }

    protected override Task<bool> LoadDataAsync()
    {
        if (Dates.Count == 0)
        {
            for (var i = 0; i < 7; i++)
            {
                var date = DateTime.Today.AddDays(i);
                _dates.Add(date);
                var title = i switch { 0 => "Today", 1 => "Tomorrow", _ => date.ToString("ddd d MMM") };
                Dates.Add(new SegmentItem(date.ToString("yyyy-MM-dd"), title, OnDateSelected));
            }

            var initial = _cart.PickupDate?.Date ?? DateTime.Today;
            if (initial < DateTime.Today || !AvailableSlots(initial).Any())
                initial = DateTime.Today.AddDays(1);
            SelectDate(initial, _cart.PickupSlot);
        }

        return Task.FromResult(true);
    }

    private void OnDateSelected(SegmentItem item)
    {
        if (DateTime.TryParse(item.Key, out var date))
            SelectDate(date, null);
    }

    private void SelectDate(DateTime date, string? preselectSlot)
    {
        SelectedDate = date;
        foreach (var d in Dates)
            d.IsSelected = d.Key == date.ToString("yyyy-MM-dd");

        Slots.Clear();
        var available = AvailableSlots(date).ToHashSet();
        foreach (var slot in PickupSlots.All)
        {
            var label = PickupSlots.Label(slot);
            var item = new SlotItem(label, available.Contains(label), SelectSlot);
            Slots.Add(item);
        }

        SelectedSlot = null;
        var match = Slots.FirstOrDefault(s => s.IsAvailable && s.Label == preselectSlot);
        if (match is not null)
            SelectSlot(match);
        HasNoSlots = Slots.All(s => !s.IsAvailable);
    }

    private static IEnumerable<string> AvailableSlots(DateTime date)
    {
        // Same-day pickups need at least an hour's notice before the window starts.
        var cutoff = DateTime.Now.AddHours(1);
        return PickupSlots.All
            .Where(s => date.Date > DateTime.Today || date.Date.Add(s.Start) >= cutoff)
            .Select(PickupSlots.Label);
    }

    private void SelectSlot(SlotItem slot)
    {
        foreach (var s in Slots)
            s.IsSelected = ReferenceEquals(s, slot);
        SelectedSlot = slot;
    }

    private bool CanContinue() => SelectedSlot is not null;

    [RelayCommand(CanExecute = nameof(CanContinue))]
    private Task ContinueAsync()
    {
        _cart.PickupDate = SelectedDate.Date;
        _cart.PickupSlot = SelectedSlot!.Label;
        return _mode == "edit" ? Navigation.GoBackAsync() : Navigation.GoToAsync(Routes.OrderSummary);
    }
}

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

/// <summary>C14 – Payment (Paystack).</summary>
public sealed partial class PaymentViewModel(
    ViewModelServices services, ICustomerOrderService orders, IPaymentService payments, ICartService cart)
    : BaseViewModel(services), IQueryAttributable
{
    private Guid _orderId;

    [ObservableProperty] private string orderNumber = string.Empty;
    [ObservableProperty] private string providerName = string.Empty;
    [ObservableProperty] private decimal total;
    [ObservableProperty] private string payButtonText = "Pay";
    [ObservableProperty] private bool isProcessing;
    [ObservableProperty] private bool hasError;
    [ObservableProperty] private string paymentError = string.Empty;

    public string CurrencyCode => Services.Settings.Currency.Code;

    public void ApplyQueryAttributes(IDictionary<string, object> query)
    {
        if (TryGetGuid(query, Routes.OrderIdKey, out var id))
            _orderId = id;
    }

    protected override async Task<bool> LoadDataAsync()
    {
        var detail = await orders.GetOrderAsync(_orderId);
        if (detail.IsPaid)
        {
            await Navigation.GoToAsync($"../{Routes.OrderConfirmed}", Param(Routes.OrderIdKey, _orderId));
            return true;
        }

        OrderNumber = detail.Order.OrderNumber;
        ProviderName = detail.ProviderName;
        Total = detail.Order.TotalAmount;
        PayButtonText = $"Pay {CurrencyFormatter.Current.Format(Total)}";
        return true;
    }

    [RelayCommand]
    private async Task PayAsync()
    {
        if (IsProcessing)
            return;

        try
        {
            IsProcessing = true;
            HasError = false;
            var result = await payments.PayAsync(_orderId);
            if (!result.Success)
            {
                PaymentError = result.Message ?? "Payment was not completed. Please try again.";
                HasError = true;
                return;
            }

            cart.Clear();
            // Reset the stack to Home, then show the confirmation.
            await Navigation.GoToAsync($"{Routes.CustomerHome}/{Routes.OrderConfirmed}", Param(Routes.OrderIdKey, _orderId));
        }
        catch (Exception ex)
        {
            PaymentError = ex.Message;
            HasError = true;
        }
        finally
        {
            IsProcessing = false;
        }
    }
}
