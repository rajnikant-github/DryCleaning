using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using DryCleaning.Helpers;
using DryCleaning.Models;
using DryCleaning.Services;
using DryCleaning.ViewModels.Items;

namespace DryCleaning.ViewModels.Provider;

/// <summary>Product → Service Items.</summary>
public sealed partial class ProductDetailViewModel(ViewModelServices services, IProviderCatalogService catalog)
    : BaseViewModel(services), IQueryAttributable
{
    private Guid _productId;

    public ObservableCollection<ServiceItemRow> ServiceItems { get; } = [];

    [ObservableProperty] private string productName = string.Empty;
    [ObservableProperty] private string productDescription = string.Empty;
    [ObservableProperty] private string productIcon = "🧺";
    [ObservableProperty] private string categoryName = string.Empty;
    [ObservableProperty] private bool productIsActive;
    [ObservableProperty] private bool hasServiceItems;

    public void ApplyQueryAttributes(IDictionary<string, object> query)
    {
        if (TryGetGuid(query, Routes.ProductIdKey, out var id))
            _productId = id;
    }

    protected override async Task<bool> LoadDataAsync()
    {
        var product = await catalog.GetProductAsync(_productId);
        var categories = await catalog.GetProviderCategoriesAsync();

        ProductName = product.Name;
        Title = product.Name;
        ProductDescription = product.Description;
        ProductIcon = CatalogIcons.ForProduct(product.Name);
        CategoryName = categories.FirstOrDefault(c => c.Id == product.CategoryId)?.Name ?? string.Empty;
        ProductIsActive = product.IsActive;

        var items = await catalog.GetServiceItemsAsync(_productId);
        ServiceItems.Clear();
        foreach (var item in items)
            ServiceItems.Add(new ServiceItemRow(item, EditServiceItemAsync, SetServiceItemActiveAsync));
        HasServiceItems = ServiceItems.Count > 0;

        return true;
    }

    private Task EditServiceItemAsync(ServiceItemRow row) =>
        Navigation.GoToAsync(Routes.ServiceItemEdit, new Dictionary<string, object>
        {
            [Routes.ProductIdKey] = _productId,
            [Routes.ServiceItemIdKey] = row.Item.Id
        });

    private async Task SetServiceItemActiveAsync(ServiceItemRow row, bool isActive)
    {
        try
        {
            await catalog.SetServiceItemActiveAsync(row.Item.Id, isActive);
        }
        catch (Exception ex)
        {
            row.SetActiveSilently(!isActive);
            await Dialogs.AlertAsync("Couldn't update service item", ex.Message);
        }
    }

    [RelayCommand]
    private Task AddServiceItemAsync() =>
        Navigation.GoToAsync(Routes.ServiceItemEdit, Param(Routes.ProductIdKey, _productId));

    [RelayCommand]
    private Task EditProductAsync() =>
        Navigation.GoToAsync(Routes.ProductEdit, Param(Routes.ProductIdKey, _productId));
}
