using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using DryCleaning.Helpers;
using DryCleaning.Models;
using DryCleaning.Services;
using DryCleaning.ViewModels.Items;

namespace DryCleaning.ViewModels.Provider;

/// <summary>Add / edit a Product. Providers can't create global Categories.</summary>
public sealed partial class ProductEditViewModel(ViewModelServices services, IProviderCatalogService catalog)
    : BaseViewModel(services), IQueryAttributable
{
    private Guid _productId;
    private Guid _categoryId;
    private IReadOnlyList<Category> _categories = [];

    public ObservableCollection<string> CategoryNames { get; } = [];

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(SaveCommand))]
    private string name = string.Empty;

    [ObservableProperty] private string description = string.Empty;
    [ObservableProperty] private int selectedCategoryIndex = -1;
    [ObservableProperty] private bool isActive = true;
    [ObservableProperty] private bool isNew = true;

    public void ApplyQueryAttributes(IDictionary<string, object> query)
    {
        if (TryGetGuid(query, Routes.ProductIdKey, out var productId))
            _productId = productId;
        if (TryGetGuid(query, Routes.CategoryIdKey, out var categoryId))
            _categoryId = categoryId;
    }

    protected override async Task<bool> LoadDataAsync()
    {
        _categories = await catalog.GetProviderCategoriesAsync();
        CategoryNames.Clear();
        foreach (var category in _categories)
            CategoryNames.Add(category.Name);

        IsNew = _productId == Guid.Empty;
        Title = IsNew ? "Add Product" : "Edit Product";

        if (!IsNew)
        {
            var product = await catalog.GetProductAsync(_productId);
            Name = product.Name;
            Description = product.Description;
            IsActive = product.IsActive;
            _categoryId = product.CategoryId;
        }

        SelectedCategoryIndex = _categories.ToList().FindIndex(c => c.Id == _categoryId);
        if (SelectedCategoryIndex < 0 && _categories.Count > 0)
            SelectedCategoryIndex = 0;

        return true;
    }

    private bool CanSave() => !string.IsNullOrWhiteSpace(Name);

    [RelayCommand(CanExecute = nameof(CanSave))]
    private Task SaveAsync() => RunBusyAsync(async () =>
    {
        if (SelectedCategoryIndex < 0 || SelectedCategoryIndex >= _categories.Count)
        {
            await Dialogs.AlertAsync("Category required", "Choose a category for this product.");
            return;
        }

        var product = IsNew ? new Product() : await catalog.GetProductAsync(_productId);
        var toSave = new Product
        {
            Id = product.Id,
            Name = Name.Trim(),
            Description = Description.Trim(),
            CategoryId = _categories[SelectedCategoryIndex].Id,
            IsActive = IsActive
        };

        var saved = await catalog.SaveProductAsync(toSave);
        if (IsNew)
        {
            // Go straight to the new product so the provider can add Service Items.
            await Navigation.GoToAsync($"../{Routes.ProductDetail}", Param(Routes.ProductIdKey, saved.Id));
        }
        else
        {
            await Navigation.GoBackAsync();
        }
    });
}
