using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using DryCleaning.Helpers;
using DryCleaning.Models;
using DryCleaning.Services;
using DryCleaning.ViewModels.Items;

namespace DryCleaning.ViewModels.Provider;

/// <summary>P11 – Services / catalog (Category → Product).</summary>
public sealed partial class CatalogViewModel(ViewModelServices services, IProviderCatalogService catalog)
    : BaseViewModel(services)
{
    private Guid _selectedCategoryId;

    public ObservableCollection<SegmentItem> Categories { get; } = [];
    public ObservableCollection<ProductCardItem> Products { get; } = [];

    [ObservableProperty] private string selectedCategoryName = string.Empty;

    protected override async Task<bool> LoadDataAsync()
    {
        var categories = await catalog.GetProviderCategoriesAsync();
        if (categories.Count == 0)
            return false;

        if (_selectedCategoryId == Guid.Empty || categories.All(c => c.Id != _selectedCategoryId))
            _selectedCategoryId = categories[0].Id;

        Categories.Clear();
        foreach (var category in categories)
        {
            Categories.Add(new SegmentItem(category.Id.ToString(), $"{CatalogIcons.ForCategory(category.Name)} {category.Name}", OnCategorySelected)
            {
                IsSelected = category.Id == _selectedCategoryId
            });
        }

        SelectedCategoryName = categories.First(c => c.Id == _selectedCategoryId).Name;
        await LoadProductsAsync();
        return true;
    }

    private async Task LoadProductsAsync()
    {
        var products = await catalog.GetProductsAsync(_selectedCategoryId);
        Products.Clear();
        foreach (var product in products)
            Products.Add(new ProductCardItem(product, OpenProductAsync, SetProductActiveAsync));
    }

    private async void OnCategorySelected(SegmentItem segment)
    {
        if (!Guid.TryParse(segment.Key, out var id) || id == _selectedCategoryId)
            return;

        _selectedCategoryId = id;
        foreach (var item in Categories)
            item.IsSelected = item.Key == segment.Key;
        SelectedCategoryName = segment.Title;

        try
        {
            await LoadProductsAsync();
        }
        catch (Exception ex)
        {
            await Dialogs.AlertAsync("Couldn't load products", ex.Message);
        }
    }

    private Task OpenProductAsync(ProductCardItem item) =>
        Navigation.GoToAsync(Routes.ProductDetail, Param(Routes.ProductIdKey, item.Id));

    private async Task SetProductActiveAsync(ProductCardItem item, bool isActive)
    {
        try
        {
            await catalog.SetProductActiveAsync(item.Id, isActive);
        }
        catch (Exception ex)
        {
            item.SetActiveSilently(!isActive);
            await Dialogs.AlertAsync("Couldn't update product", ex.Message);
        }
    }

    [RelayCommand]
    private Task AddProductAsync() =>
        Navigation.GoToAsync(Routes.ProductEdit, Param(Routes.CategoryIdKey, _selectedCategoryId));
}
