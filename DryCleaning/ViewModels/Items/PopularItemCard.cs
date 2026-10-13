using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using DryCleaning.Helpers;
using DryCleaning.Models;
using DryCleaning.Services;

namespace DryCleaning.ViewModels.Items;

public sealed class PopularItemCard(PopularServiceItem item, Func<PopularServiceItem, Task> open)
{
    public string Icon => CatalogIcons.ForProduct(item.ProductName);
    public string Title => $"{item.ProductName} · {item.ServiceItemName}";
    public string Subtitle => item.ProviderName;
    public decimal Price => item.Price;
    public IAsyncRelayCommand OpenCommand { get; } = new AsyncRelayCommand(() => open(item));
}
