using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using DryCleaning.Helpers;
using DryCleaning.Models;
using DryCleaning.Services;

namespace DryCleaning.ViewModels.Items;

// Item ViewModels for the Customer screens (each carries its own commands).

public sealed class CategoryCardItem(Category category, Func<Category, Task> open)
{
    public Guid Id => category.Id;
    public string Name => category.Name;
    public string Description => category.Description;
    public string Icon => CatalogIcons.ForCategory(category.Name);
    public IAsyncRelayCommand OpenCommand { get; } = new AsyncRelayCommand(() => open(category));
}

public sealed class PopularItemCard(PopularServiceItem item, Func<PopularServiceItem, Task> open)
{
    public string Icon => CatalogIcons.ForProduct(item.ProductName);
    public string Title => $"{item.ProductName} · {item.ServiceItemName}";
    public string Subtitle => item.ProviderName;
    public decimal Price => item.Price;
    public IAsyncRelayCommand OpenCommand { get; } = new AsyncRelayCommand(() => open(item));
}

public sealed class ProviderCardItem(ProviderListing provider, Func<ProviderListing, Task> open)
{
    public string Name => provider.BusinessName;
    public string Initials => StringHelpers.Initials(provider.BusinessName);
    public string Description => provider.Description ?? string.Empty;
    public string LocalityText => string.IsNullOrEmpty(provider.Locality) ? "Cape Town" : $"📍 {provider.Locality}";
    public string CategoriesText => string.Join(" · ", provider.Categories);
    public string StartingPriceText => provider.StartingPrice is { } price ? $"from {CurrencyFormatter.Current.Format(price)}" : string.Empty;
    public string TurnaroundText => provider.FastestDays switch
    {
        null => string.Empty,
        1 => "⏱ Next day available",
        var d => $"⏱ From {d} days"
    };
    public IAsyncRelayCommand OpenCommand { get; } = new AsyncRelayCommand(() => open(provider));
}

public sealed class ProductRow(CatalogProduct product, Func<CatalogProduct, Task> open)
{
    public string Icon => CatalogIcons.ForProduct(product.Name);
    public string Name => product.Name;
    public string Description => product.Description ?? string.Empty;
    public string StartingPriceText => product.StartingPrice is { } price ? $"From {CurrencyFormatter.Current.Format(price)}" : string.Empty;
    public string OptionsText => product.ServiceItems.Count == 1 ? "1 option" : $"{product.ServiceItems.Count} options";
    public IAsyncRelayCommand OpenCommand { get; } = new AsyncRelayCommand(() => open(product));
}

public sealed partial class ServiceItemOption : ObservableObject
{
    public ServiceItemOption(ServiceItem item, Action<ServiceItemOption> select)
    {
        Item = item;
        SelectCommand = new RelayCommand(() => select(this));
    }

    public ServiceItem Item { get; }
    public string Name => Item.Name;
    public string Description => Item.Description;
    public decimal Price => Item.Price;
    public string EstimatedText => Item.EstimatedDays == 1 ? "Estimated: 1 day" : $"Estimated: {Item.EstimatedDays} days";
    public IRelayCommand SelectCommand { get; }

    [ObservableProperty]
    private bool isSelected;
}

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

public sealed class PredictionItem(PlacePrediction prediction, Func<PlacePrediction, Task> select)
{
    public string MainText => prediction.MainText;
    public string SecondaryText => prediction.SecondaryText;
    public IAsyncRelayCommand SelectCommand { get; } = new AsyncRelayCommand(() => select(prediction));
}

public sealed partial class SlotItem : ObservableObject
{
    public SlotItem(string label, bool isAvailable, Action<SlotItem> select)
    {
        Label = label;
        IsAvailable = isAvailable;
        SelectCommand = new RelayCommand(() => { if (IsAvailable) select(this); });
    }

    public string Label { get; }
    public bool IsAvailable { get; }
    public bool IsUnavailable => !IsAvailable;
    public IRelayCommand SelectCommand { get; }

    [ObservableProperty]
    private bool isSelected;
}
