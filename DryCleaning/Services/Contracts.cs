using DryCleaning.Models;

namespace DryCleaning.Services;

// ── App / platform services ──────────────────────────────────────────────────

public interface INavigationService
{
    Task GoToAsync(string route, IDictionary<string, object>? parameters = null);
    Task GoBackAsync();
}

public interface IDialogService
{
    Task AlertAsync(string title, string message, string cancel = "OK");
    Task<bool> ConfirmAsync(string title, string message, string accept = "Yes", string cancel = "Cancel");
    Task<string?> PromptAsync(string title, string message, string accept = "Submit", string placeholder = "", string initialValue = "");
    Task<string?> ActionSheetAsync(string title, string cancel, params string[] buttons);
}

public interface IPhotoService
{
    /// <summary>Lets the user take or choose a photo. Returns a local file path, or null if cancelled.</summary>
    Task<string?> AddPhotoAsync();
}

public interface ISessionService
{
    UserRole SelectedRole { get; set; }
    User? CurrentUser { get; }

    /// <summary>Customer / Provider / Driver profile id of the signed-in user.</summary>
    Guid? ProfileId { get; }
    Guid? ProviderId { get; }
    Guid? CustomerId { get; }
    bool IsSignedIn { get; }
    void SignIn(User user, Guid? profileId);
    void SignOut();
}

// ── Domain services (mock now, API later) ────────────────────────────────────

public sealed record AuthResult(bool Success, string? Error = null)
{
    public static AuthResult Ok() => new(true);
    public static AuthResult Fail(string error) => new(false, error);
}

public interface IAuthService
{
    Task<AuthResult> LoginAsync(string identifier, string password, UserRole role);
    /// <summary>Sends an OTP and returns the masked destination (e.g. +27 82 *** 4567).</summary>
    Task<string> RequestOtpAsync(string identifier, UserRole role);
    Task<AuthResult> VerifyOtpAsync(string identifier, string code, UserRole role);
    Task LogoutAsync();
}

public interface IProviderOrderService
{
    Task<ProviderDashboard> GetDashboardAsync();
    Task<IReadOnlyList<OrderSummary>> GetOrdersAsync(ProviderOrderFilter filter);
    Task<IReadOnlyDictionary<ProviderOrderFilter, int>> GetOrderCountsAsync();
    Task<OrderDetail> GetOrderDetailAsync(Guid orderId);

    Task AcceptOrderAsync(Guid orderId);
    Task RejectOrderAsync(Guid orderId, string reason);
    Task ConfirmReceivedAsync(Guid orderId, ReceiveOrderRequest request);
    Task CompleteInspectionAsync(Guid orderId, IReadOnlyList<InspectionResult> results);
    Task MarkQualityCheckAsync(Guid orderId);
    Task SendBackToProcessingAsync(Guid orderId, string reason);
    Task MarkReadyForDeliveryAsync(Guid orderId, IReadOnlyList<string> photoPaths);
    Task<DriverAssignment> RequestDeliveryDriverAsync(Guid orderId);

    /// <summary>Records a discrepancy/damage report without stopping the order.</summary>
    Task ReportIssueAsync(Guid orderId, string reason, string description);

    /// <summary>Raises a dispute and puts the order on hold (status Disputed).</summary>
    Task RaiseDisputeAsync(Guid orderId, string reason, string description);

    /// <summary>Mock-only: simulates the marketplace driver collecting items from the customer.</summary>
    Task SimulateDriverPickupAsync(Guid orderId);
}

public interface IProviderCatalogService
{
    Task<IReadOnlyList<Category>> GetProviderCategoriesAsync();
    Task<IReadOnlyList<ProductSummary>> GetProductsAsync(Guid categoryId);
    Task<Product> GetProductAsync(Guid productId);
    Task<Product> SaveProductAsync(Product product);
    Task SetProductActiveAsync(Guid productId, bool isActive);
    Task<IReadOnlyList<ServiceItem>> GetServiceItemsAsync(Guid productId);
    Task<ServiceItem> GetServiceItemAsync(Guid serviceItemId);
    Task<ServiceItem> SaveServiceItemAsync(ServiceItem serviceItem);
    Task SetServiceItemActiveAsync(Guid serviceItemId, bool isActive);
}

public interface IProviderProfileService
{
    Task<ProviderProfile> GetProfileAsync();
}

public interface INotificationService
{
    Task<IReadOnlyList<Notification>> GetNotificationsAsync();
    Task<int> GetUnreadCountAsync();
    Task MarkAsReadAsync(Guid notificationId);
    Task MarkAllAsReadAsync();
}

// ── Customer ─────────────────────────────────────────────────────────────────

public interface ICustomerCatalogService
{
    Task<IReadOnlyList<Category>> GetCategoriesAsync();
    Task<Category> GetCategoryAsync(Guid categoryId);
    Task<IReadOnlyList<ProviderListing>> GetProvidersAsync(Guid categoryId);
    Task<ProviderListing> GetProviderAsync(Guid providerId);
    Task<IReadOnlyList<CatalogProduct>> GetProductsAsync(Guid providerId, Guid categoryId);
    Task<CatalogProduct> GetProductAsync(Guid productId);
    Task<IReadOnlyList<PopularServiceItem>> GetPopularItemsAsync(int take);
}

public interface ICustomerOrderService
{
    Task<CustomerHomeData> GetHomeAsync();
    Task<IReadOnlyList<OrderSummary>> GetOrdersAsync(CustomerOrderFilter filter);
    Task<IReadOnlyDictionary<CustomerOrderFilter, int>> GetOrderCountsAsync();
    Task<OrderDetail> GetOrderAsync(Guid orderId);

    /// <summary>Creates the order from the cart (prices recalculated). Returns the new order id.</summary>
    Task<Guid> PlaceOrderAsync(ICartService cart);
    Task CancelOrderAsync(Guid orderId, string reason);
    Task RaiseDisputeAsync(Guid orderId, string reason, string description);
}

public interface ICustomerProfileService
{
    Task<CustomerProfile> GetProfileAsync();
    Task UpdateProfileAsync(string name, string? email, string? phone);
    Task<Address> SaveAddressAsync(Address address);
}

/// <summary>Paystack checkout (mock now; WebView + API verification when live).</summary>
public interface IPaymentService
{
    Task<PaymentResult> PayAsync(Guid orderId);
}

/// <summary>Google Places autocomplete / details. Sample Cape Town data until an API key is configured.</summary>
public interface IPlacesService
{
    Task<IReadOnlyList<PlacePrediction>> AutocompleteAsync(string query);
    Task<Address> GetDetailsAsync(string placeId);
    Task<Address?> GetCurrentLocationAddressAsync();
}

public sealed class CartLine
{
    public Guid Id { get; } = Guid.NewGuid();
    public Guid ServiceItemId { get; init; }
    public Guid ProductId { get; init; }
    public Guid CategoryId { get; init; }
    public string CategoryName { get; init; } = string.Empty;
    public string ProductName { get; init; } = string.Empty;
    public string ServiceItemName { get; init; } = string.Empty;
    public decimal UnitPrice { get; init; }
    public int EstimatedDays { get; init; }
    public int Quantity { get; set; }
    public string? Notes { get; set; }
    public string? PhotoPath { get; set; }
    public decimal LineTotal => UnitPrice * Quantity;
}

/// <summary>Basket for one provider, plus the chosen pickup address and slot.</summary>
public interface ICartService
{
    event EventHandler? Changed;

    Guid? ProviderId { get; }
    string ProviderName { get; }
    IReadOnlyList<CartLine> Lines { get; }
    int ItemCount { get; }
    bool IsEmpty { get; }
    decimal SubTotal { get; }
    decimal DeliveryFee { get; }
    decimal Tax { get; }
    decimal Total { get; }
    int EstimatedDays { get; }

    Address? PickupAddress { get; set; }
    DateTime? PickupDate { get; set; }
    string? PickupSlot { get; set; }

    bool BelongsToOtherProvider(Guid providerId);
    void Add(Guid providerId, string providerName, CartLine line);
    void UpdateQuantity(Guid lineId, int quantity);
    void Remove(Guid lineId);
    void Clear();
}
