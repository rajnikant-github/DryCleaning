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
        Tax = _cart.Tax;
        // Pickup & delivery depends on the address, so the cart total leaves it out; Order Summary shows the full price.
        Total = _cart.SubTotal + _cart.Tax;
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
