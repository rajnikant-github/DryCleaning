using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using DryCleaning.Helpers;
using DryCleaning.Models;
using DryCleaning.Services;

namespace DryCleaning.ViewModels.Items;

public sealed class ProductRow(CatalogProduct product, Func<CatalogProduct, Task> open)
{
    public string Icon => CatalogIcons.ForProduct(product.Name);
    public string Name => product.Name;
    public string Description => product.Description ?? string.Empty;
    public string StartingPriceText => product.StartingPrice is { } price ? $"From {CurrencyFormatter.Current.Format(price)}" : string.Empty;
    public string OptionsText => product.ServiceItems.Count == 1 ? "1 option" : $"{product.ServiceItems.Count} options";
    public IAsyncRelayCommand OpenCommand { get; } = new AsyncRelayCommand(() => open(product));
}
