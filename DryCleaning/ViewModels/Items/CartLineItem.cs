using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using DryCleaning.Helpers;
using DryCleaning.Models;
using DryCleaning.Services;

namespace DryCleaning.ViewModels.Items;

public sealed partial class CartLineItem : ObservableObject
{
    private readonly ICartService _cart;
    private readonly bool _ready;

    public CartLineItem(CartLine line, ICartService cart, Func<CartLineItem, Task> remove)
    {
        Line = line;
        _cart = cart;
        quantity = line.Quantity;
        RemoveCommand = new AsyncRelayCommand(() => remove(this));
        _ready = true;
    }

    public CartLine Line { get; }
    public string Icon => CatalogIcons.ForProduct(Line.ProductName);
    public string Title => Line.ProductName;
    public string Subtitle => $"{Line.CategoryName} · {Line.ServiceItemName}";
    public string? Notes => Line.Notes;
    public bool HasNotes => !string.IsNullOrWhiteSpace(Line.Notes);
    public bool HasPhoto => !string.IsNullOrEmpty(Line.PhotoPath);
    public decimal UnitPrice => Line.UnitPrice;
    public decimal LineTotal => Line.LineTotal;
    public string QuantityText => $"Qty: {Line.Quantity}";
    public IAsyncRelayCommand RemoveCommand { get; }

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(LineTotal))]
    [NotifyPropertyChangedFor(nameof(QuantityText))]
    private int quantity;

    partial void OnQuantityChanged(int value)
    {
        if (!_ready)
            return;
        _cart.UpdateQuantity(Line.Id, value);
    }
}
