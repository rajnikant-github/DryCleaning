using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using DryCleaning.Helpers;
using DryCleaning.Models;

namespace DryCleaning.ViewModels.Items;

public sealed partial class ProductCardItem : ObservableObject
{
    private readonly Func<ProductCardItem, bool, Task> _setActive;
    private bool _suppress;

    public ProductCardItem(ProductSummary summary, Func<ProductCardItem, Task> open, Func<ProductCardItem, bool, Task> setActive)
    {
        Summary = summary;
        _setActive = setActive;
        _suppress = true;
        IsActive = summary.Product.IsActive;
        _suppress = false;
        OpenCommand = new AsyncRelayCommand(() => open(this));
    }

    public ProductSummary Summary { get; }
    public Guid Id => Summary.Product.Id;
    public string Icon => CatalogIcons.ForProduct(Summary.Product.Name);
    public string Name => Summary.Product.Name;
    public string Description => Summary.Product.Description;
    public string ServiceItemsText => Summary.ServiceItemCount == 1 ? "1 service item" : $"{Summary.ServiceItemCount} service items";
    public string StartingPriceText => Summary.StartingPrice is { } price
        ? $"From {CurrencyFormatter.Current.Format(price)}"
        : "No active service items";
    public IAsyncRelayCommand OpenCommand { get; }

    [ObservableProperty]
    private bool isActive;

    partial void OnIsActiveChanged(bool value)
    {
        if (!_suppress)
            _ = _setActive(this, value);
    }

    public void SetActiveSilently(bool value)
    {
        _suppress = true;
        IsActive = value;
        _suppress = false;
    }
}
