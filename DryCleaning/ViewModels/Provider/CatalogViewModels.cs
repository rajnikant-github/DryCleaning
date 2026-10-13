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

/// <summary>Add / edit a Service Item (price, estimated days, enable/disable).</summary>
public sealed partial class ServiceItemEditViewModel(ViewModelServices services, IProviderCatalogService catalog, ICurrencyFormatter currency)
    : BaseViewModel(services), IQueryAttributable
{
    private Guid _productId;
    private Guid _serviceItemId;

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(SaveCommand))]
    private string name = string.Empty;

    [ObservableProperty] private string description = string.Empty;

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(SaveCommand))]
    private string priceText = string.Empty;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(EstimatedDaysText))]
    private int estimatedDays = 2;

    [ObservableProperty] private bool isActive = true;
    [ObservableProperty] private bool isNew = true;
    [ObservableProperty] private string productName = string.Empty;

    public string CurrencySymbol => currency.Symbol;
    public string CurrencyCode => currency.Code;
    public string EstimatedDaysText => EstimatedDays == 1 ? "1 day" : $"{EstimatedDays} days";

    public void ApplyQueryAttributes(IDictionary<string, object> query)
    {
        if (TryGetGuid(query, Routes.ProductIdKey, out var productId))
            _productId = productId;
        if (TryGetGuid(query, Routes.ServiceItemIdKey, out var itemId))
            _serviceItemId = itemId;
    }

    protected override async Task<bool> LoadDataAsync()
    {
        var product = await catalog.GetProductAsync(_productId);
        ProductName = product.Name;
        IsNew = _serviceItemId == Guid.Empty;
        Title = IsNew ? "Add Service Item" : "Edit Service Item";

        if (!IsNew)
        {
            var item = await catalog.GetServiceItemAsync(_serviceItemId);
            Name = item.Name;
            Description = item.Description;
            PriceText = item.Price.ToString("0.00", System.Globalization.CultureInfo.InvariantCulture);
            EstimatedDays = item.EstimatedDays;
            IsActive = item.IsActive;
        }

        return true;
    }

    private bool CanSave() => !string.IsNullOrWhiteSpace(Name) && !string.IsNullOrWhiteSpace(PriceText);

    [RelayCommand(CanExecute = nameof(CanSave))]
    private Task SaveAsync() => RunBusyAsync(async () =>
    {
        if (!currency.TryParse(PriceText, out var price) || price <= 0)
        {
            await Dialogs.AlertAsync("Invalid price", $"Enter a price greater than 0 in {currency.Code}.");
            return;
        }

        await catalog.SaveServiceItemAsync(new ServiceItem
        {
            Id = IsNew ? Guid.NewGuid() : _serviceItemId,
            ProductId = _productId,
            Name = Name.Trim(),
            Description = Description.Trim(),
            Price = Math.Round(price, 2),
            EstimatedDays = Math.Max(1, EstimatedDays),
            IsActive = IsActive
        });

        await Navigation.GoBackAsync();
    });
}
