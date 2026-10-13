using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using DryCleaning.Helpers;
using DryCleaning.Models;
using DryCleaning.Services;
using DryCleaning.ViewModels.Items;

namespace DryCleaning.ViewModels.Customer;

/// <summary>C07 – Products of a provider in a category.</summary>
public sealed partial class ProductsViewModel(ViewModelServices services, ICustomerCatalogService catalog, ICartService cart)
    : CartAwareViewModel(services, cart), IQueryAttributable
{
    private Guid _providerId;
    private Guid _categoryId;

    public ObservableCollection<ProductRow> Products { get; } = [];

    [ObservableProperty] private string providerName = string.Empty;
    [ObservableProperty] private string providerInitials = string.Empty;
    [ObservableProperty] private string providerInfo = string.Empty;
    [ObservableProperty] private string categoryName = string.Empty;

    public void ApplyQueryAttributes(IDictionary<string, object> query)
    {
        if (TryGetGuid(query, Routes.ProviderIdKey, out var providerId))
            _providerId = providerId;
        if (TryGetGuid(query, Routes.CategoryIdKey, out var categoryId))
            _categoryId = categoryId;
    }

    protected override async Task<bool> LoadDataAsync()
    {
        var provider = await catalog.GetProviderAsync(_providerId);
        var category = await catalog.GetCategoryAsync(_categoryId);
        ProviderName = provider.BusinessName;
        ProviderInitials = StringHelpers.Initials(provider.BusinessName);
        ProviderInfo = string.IsNullOrEmpty(provider.Locality) ? provider.Description ?? string.Empty : $"📍 {provider.Locality} · {provider.Description}";
        CategoryName = category.Name;
        Title = category.Name;

        var products = await catalog.GetProductsAsync(_providerId, _categoryId);
        Products.Clear();
        foreach (var product in products)
            Products.Add(new ProductRow(product, p => Navigation.GoToAsync(Routes.ServiceItems, new Dictionary<string, object>
            {
                [Routes.ProductIdKey] = p.Id,
                [Routes.ProviderIdKey] = _providerId
            })));
        return Products.Count > 0;
    }
}
