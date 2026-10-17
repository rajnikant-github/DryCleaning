using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using DryCleaning.Helpers;
using DryCleaning.Models;
using DryCleaning.Services;
using DryCleaning.ViewModels.Items;

namespace DryCleaning.ViewModels.Customer;

/// <summary>Providers offering the chosen category (every order belongs to one provider).</summary>
public sealed partial class ProvidersViewModel(ViewModelServices services, ICustomerCatalogService catalog, ICartService cart)
    : CartAwareViewModel(services, cart), IQueryAttributable
{
    private Guid _categoryId;

    public ObservableCollection<ProviderCardItem> Providers { get; } = [];

    [ObservableProperty] private string categoryName = string.Empty;
    [ObservableProperty] private string categoryIcon = AppIcons.Shirt;

    public void ApplyQueryAttributes(IDictionary<string, object> query)
    {
        if (TryGetGuid(query, Routes.CategoryIdKey, out var id))
            _categoryId = id;
    }

    protected override async Task<bool> LoadDataAsync()
    {
        var category = await catalog.GetCategoryAsync(_categoryId);
        CategoryName = category.Name;
        CategoryIcon = CatalogIcons.ForCategory(category.Name);
        Title = category.Name;

        var providers = await catalog.GetProvidersAsync(_categoryId);
        Providers.Clear();
        foreach (var provider in providers)
            Providers.Add(new ProviderCardItem(provider, OpenProviderAsync));
        return Providers.Count > 0;
    }

    private Task OpenProviderAsync(ProviderListing provider) =>
        Navigation.GoToAsync(Routes.Products, new Dictionary<string, object>
        {
            [Routes.ProviderIdKey] = provider.Id,
            [Routes.CategoryIdKey] = _categoryId
        });
}
