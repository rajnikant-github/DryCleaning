using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using DryCleaning.Helpers;
using DryCleaning.Models;
using DryCleaning.Services;
using DryCleaning.ViewModels.Items;

namespace DryCleaning.ViewModels.Customer;

/// <summary>C09 – Quantity, special instructions and optional photo.</summary>
public sealed partial class ItemDetailsViewModel(
    ViewModelServices services, ICustomerCatalogService catalog, ICartService cart, IPhotoService photos)
    : CartAwareViewModel(services, cart), IQueryAttributable
{
    private Guid _serviceItemId;
    private Guid _productId;
    private Guid _providerId;
    private bool _quickMode;
    private CatalogProduct? _product;
    private ServiceItem? _item;
    private string _providerName = string.Empty;

    public ObservableCollection<string> Photos { get; } = [];

    [ObservableProperty] private string productName = string.Empty;
    [ObservableProperty] private string productIcon = AppIcons.BasketShopping;
    [ObservableProperty] private string serviceItemName = string.Empty;
    [ObservableProperty] private string categoryName = string.Empty;
    [ObservableProperty] private string providerName = string.Empty;
    [ObservableProperty] private string estimatedText = string.Empty;
    [ObservableProperty] private decimal unitPrice;
    [ObservableProperty] private string notes = string.Empty;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(LineTotal))]
    private int quantity = 1;

    public decimal LineTotal => UnitPrice * Quantity;

    public void ApplyQueryAttributes(IDictionary<string, object> query)
    {
        if (TryGetGuid(query, Routes.ServiceItemIdKey, out var itemId))
            _serviceItemId = itemId;
        if (TryGetGuid(query, Routes.ProductIdKey, out var productId))
            _productId = productId;
        if (TryGetGuid(query, Routes.ProviderIdKey, out var providerId))
            _providerId = providerId;
        _quickMode = query.TryGetValue(Routes.ModeKey, out var mode) && mode as string == "quick";
    }

    protected override async Task<bool> LoadDataAsync()
    {
        _product = await catalog.GetProductAsync(_productId);
        _item = _product.ServiceItems.FirstOrDefault(s => s.Id == _serviceItemId)
                ?? throw new InvalidOperationException("This service is no longer available.");
        _providerName = (await catalog.GetProviderAsync(_providerId)).BusinessName;

        ProductName = _product.Name;
        ProductIcon = CatalogIcons.ForProduct(_product.Name);
        CategoryName = _product.CategoryName;
        ServiceItemName = _item.Name;
        ProviderName = _providerName;
        UnitPrice = _item.Price;
        EstimatedText = _item.EstimatedDays == 1 ? "Ready in about 1 day" : $"Ready in about {_item.EstimatedDays} days";
        Title = _product.Name;
        OnPropertyChanged(nameof(LineTotal));
        return true;
    }

    [RelayCommand]
    private async Task AddPhotoAsync()
    {
        var path = await photos.AddPhotoAsync();
        if (path is not null)
            Photos.Add(path);
    }

    [RelayCommand]
    private Task AddItemAsync() => AddToCartAsync(goToCart: false);

    [RelayCommand]
    private Task ContinueAsync() => AddToCartAsync(goToCart: true);

    private Task AddToCartAsync(bool goToCart) => RunBusyAsync(async () =>
    {
        if (_product is null || _item is null || Quantity < 1)
            return;

        if (Cart.BelongsToOtherProvider(_providerId) &&
            !await Dialogs.ConfirmAsync("Start a new cart?",
                $"Your cart has items from {Cart.ProviderName}. An order can only be placed with one provider.",
                "Start new cart", "Keep cart"))
        {
            return;
        }

        Cart.Add(_providerId, _providerName, new CartLine
        {
            ServiceItemId = _item.Id,
            ProductId = _product.Id,
            CategoryId = _product.CategoryId,
            CategoryName = _product.CategoryName,
            ProductName = _product.Name,
            ServiceItemName = _item.Name,
            UnitPrice = _item.Price,
            EstimatedDays = _item.EstimatedDays,
            Quantity = Quantity,
            Notes = string.IsNullOrWhiteSpace(Notes) ? null : Notes.Trim(),
            PhotoPath = Photos.FirstOrDefault()
        });
        RefreshCart();

        if (goToCart)
            await Navigation.GoToAsync($"../{Routes.Cart}");
        else
            await Navigation.GoToAsync(_quickMode ? ".." : "../..");
    });
}
