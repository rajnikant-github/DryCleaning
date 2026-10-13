using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using DryCleaning.Helpers;
using DryCleaning.Models;
using DryCleaning.Services;
using DryCleaning.ViewModels.Items;

namespace DryCleaning.ViewModels.Customer;

/// <summary>Shows a "View cart" bar on browse screens.</summary>
public abstract partial class CartAwareViewModel : BaseViewModel
{
    protected CartAwareViewModel(ViewModelServices services, ICartService cart) : base(services)
    {
        Cart = cart;
    }

    protected ICartService Cart { get; }

    [ObservableProperty] private bool hasCart;
    [ObservableProperty] private string cartSummaryText = string.Empty;

    public override Task OnAppearingAsync()
    {
        RefreshCart();
        return base.OnAppearingAsync();
    }

    protected void RefreshCart()
    {
        HasCart = !Cart.IsEmpty;
        CartSummaryText = Cart.ItemCount == 1
            ? $"1 item · {CurrencyFormatter.Current.Format(Cart.SubTotal)}"
            : $"{Cart.ItemCount} items · {CurrencyFormatter.Current.Format(Cart.SubTotal)}";
    }

    [RelayCommand]
    private Task OpenCartAsync() => Navigation.GoToAsync(Routes.Cart);
}

/// <summary>C05 – Customer home.</summary>
public sealed partial class CustomerHomeViewModel(ViewModelServices services, ICustomerOrderService orders, ICartService cart)
    : CartAwareViewModel(services, cart)
{
    public ObservableCollection<CategoryCardItem> Categories { get; } = [];
    public ObservableCollection<OrderCardItem> RecentOrders { get; } = [];
    public ObservableCollection<PopularItemCard> PopularItems { get; } = [];

    [ObservableProperty] private string greeting = string.Empty;
    [ObservableProperty] private string firstName = string.Empty;
    [ObservableProperty] private string addressText = "Add your pickup address";
    [ObservableProperty] private bool hasUnread;
    [ObservableProperty] private OrderCardItem? activeOrder;
    [ObservableProperty] private bool hasActiveOrder;
    [ObservableProperty] private bool hasRecentOrders;

    protected override async Task<bool> LoadDataAsync()
    {
        var home = await orders.GetHomeAsync();
        Greeting = TimeFormat.Greeting(DateTime.Now) + ",";
        FirstName = home.FirstName;
        AddressText = home.Address?.FormattedAddress ?? "Add your pickup address";
        HasUnread = home.UnreadNotifications > 0;

        Categories.Clear();
        foreach (var category in home.Categories)
            Categories.Add(new CategoryCardItem(category, OpenCategoryAsync));

        ActiveOrder = home.ActiveOrder is null ? null : CreateOrderItem(home.ActiveOrder);
        HasActiveOrder = ActiveOrder is not null;

        RecentOrders.Clear();
        foreach (var order in home.RecentOrders)
            RecentOrders.Add(CreateOrderItem(order));
        HasRecentOrders = RecentOrders.Count > 0;

        PopularItems.Clear();
        foreach (var item in home.PopularItems)
            PopularItems.Add(new PopularItemCard(item, OpenPopularAsync));

        RefreshCart();
        return true;
    }

    private OrderCardItem CreateOrderItem(OrderSummary order) =>
        new(order, OpenOrderAsync, title: order.ProviderName, hint: order.Status.CustomerStatusMessage(order.IsPaid));

    private Task OpenOrderAsync(OrderSummary order) =>
        Navigation.GoToAsync(CustomerFlow.IsActive(order.Status) ? Routes.OrderTracking : Routes.CustomerOrderDetail,
            Param(Routes.OrderIdKey, order.Id));

    private Task OpenCategoryAsync(Category category) =>
        Navigation.GoToAsync(Routes.Providers, Param(Routes.CategoryIdKey, category.Id));

    private Task OpenPopularAsync(PopularServiceItem item) =>
        Navigation.GoToAsync(Routes.ItemDetails, new Dictionary<string, object>
        {
            [Routes.ServiceItemIdKey] = item.ServiceItemId,
            [Routes.ProductIdKey] = item.ProductId,
            [Routes.ProviderIdKey] = item.ProviderId,
            [Routes.ModeKey] = "quick"
        });

    [RelayCommand]
    private Task QuickOrderAsync() => Navigation.GoToAsync(Routes.Categories);

    [RelayCommand]
    private Task OpenNotificationsAsync() => Navigation.GoToAsync(Routes.Notifications);

    [RelayCommand]
    private Task ChangeAddressAsync() => Navigation.GoToAsync(Routes.PickupAddress, Param(Routes.ModeKey, "profile"));

    [RelayCommand]
    private Task ViewAllOrdersAsync() => Navigation.GoToAsync(Routes.CustomerOrders);
}

/// <summary>C06 – Choose a category.</summary>
public sealed partial class CategoriesViewModel(ViewModelServices services, ICustomerCatalogService catalog, ICartService cart)
    : CartAwareViewModel(services, cart)
{
    public ObservableCollection<CategoryCardItem> Categories { get; } = [];

    protected override async Task<bool> LoadDataAsync()
    {
        var categories = await catalog.GetCategoriesAsync();
        Categories.Clear();
        foreach (var category in categories)
            Categories.Add(new CategoryCardItem(category, c => Navigation.GoToAsync(Routes.Providers, Param(Routes.CategoryIdKey, c.Id))));
        return Categories.Count > 0;
    }
}

/// <summary>Providers offering the chosen category (every order belongs to one provider).</summary>
public sealed partial class ProvidersViewModel(ViewModelServices services, ICustomerCatalogService catalog, ICartService cart)
    : CartAwareViewModel(services, cart), IQueryAttributable
{
    private Guid _categoryId;

    public ObservableCollection<ProviderCardItem> Providers { get; } = [];

    [ObservableProperty] private string categoryName = string.Empty;
    [ObservableProperty] private string categoryIcon = "👔";

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

/// <summary>C08 – Service Items of a product (Category → Product → Service Item).</summary>
public sealed partial class ServiceItemsViewModel(ViewModelServices services, ICustomerCatalogService catalog, ICartService cart)
    : CartAwareViewModel(services, cart), IQueryAttributable
{
    private Guid _productId;
    private Guid _providerId;

    public ObservableCollection<ServiceItemOption> Options { get; } = [];

    [ObservableProperty] private string productName = string.Empty;
    [ObservableProperty] private string productIcon = "🧺";
    [ObservableProperty] private string productDescription = string.Empty;

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(ContinueCommand))]
    private ServiceItemOption? selectedOption;

    public void ApplyQueryAttributes(IDictionary<string, object> query)
    {
        if (TryGetGuid(query, Routes.ProductIdKey, out var productId))
            _productId = productId;
        if (TryGetGuid(query, Routes.ProviderIdKey, out var providerId))
            _providerId = providerId;
    }

    protected override async Task<bool> LoadDataAsync()
    {
        var product = await catalog.GetProductAsync(_productId);
        ProductName = product.Name;
        Title = product.Name;
        ProductIcon = CatalogIcons.ForProduct(product.Name);
        ProductDescription = product.Description ?? string.Empty;

        var previous = SelectedOption?.Item.Id;
        Options.Clear();
        foreach (var item in product.ServiceItems)
            Options.Add(new ServiceItemOption(item, Select) { IsSelected = item.Id == previous });
        SelectedOption = Options.FirstOrDefault(o => o.IsSelected);
        return Options.Count > 0;
    }

    private void Select(ServiceItemOption option)
    {
        foreach (var o in Options)
            o.IsSelected = ReferenceEquals(o, option);
        SelectedOption = option;
    }

    private bool CanContinue() => SelectedOption is not null;

    [RelayCommand(CanExecute = nameof(CanContinue))]
    private Task ContinueAsync() =>
        Navigation.GoToAsync(Routes.ItemDetails, new Dictionary<string, object>
        {
            [Routes.ServiceItemIdKey] = SelectedOption!.Item.Id,
            [Routes.ProductIdKey] = _productId,
            [Routes.ProviderIdKey] = _providerId
        });
}

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
    [ObservableProperty] private string productIcon = "🧺";
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

public static class CustomerFlow
{
    public static bool IsActive(OrderStatus status) =>
        status is not (OrderStatus.Completed or OrderStatus.Delivered or OrderStatus.Cancelled);
}
