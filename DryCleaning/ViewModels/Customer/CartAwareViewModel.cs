using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using DryCleaning.Helpers;
using DryCleaning.Models;
using DryCleaning.Services;
using DryCleaning.ViewModels.Items;

namespace DryCleaning.ViewModels.Customer;

/// <summary>Shows a "View cart" bar on browse screens.</summary>
public abstract partial class CartAwareViewModel : BaseViewModel
{
    protected CartAwareViewModel(ViewModelServices services, ICartService cart) : base(services)
    {
        Cart = cart;
    }

    protected ICartService Cart { get; }

    [ObservableProperty] private bool hasCart;
    [ObservableProperty] private string cartSummaryText = string.Empty;

    public override Task OnAppearingAsync()
    {
        RefreshCart();
        return base.OnAppearingAsync();
    }

    protected void RefreshCart()
    {
        HasCart = !Cart.IsEmpty;
        CartSummaryText = Cart.ItemCount == 1
            ? $"1 item · {CurrencyFormatter.Current.Format(Cart.SubTotal)}"
            : $"{Cart.ItemCount} items · {CurrencyFormatter.Current.Format(Cart.SubTotal)}";
    }

    [RelayCommand]
    private Task OpenCartAsync() => Navigation.GoToAsync(Routes.Cart);
}
