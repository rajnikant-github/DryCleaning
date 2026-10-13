using DryCleaning.Api.Domain;

namespace DryCleaning.Api.Dtos;

// ═════════════════════════════════════════════════════════════════════════════
// Auth
// ═════════════════════════════════════════════════════════════════════════════

public sealed record RegisterCustomerRequest(string Name, string? Email, string? Phone, string Password);

public sealed record RegisterProviderRequest(
    string Name, string? Email, string? Phone, string Password,
    string BusinessName, string? Description, IReadOnlyList<Guid>? CategoryIds);

public sealed record RegisterDriverRequest(string Name, string? Email, string? Phone, string Password, string LicenseNumber);

/// <param name="Identifier">Mobile number or email.</param>
public sealed record LoginRequest(string Identifier, string Password);

public sealed record RequestOtpRequest(string Identifier);

/// <param name="DevCode">Only returned when Otp:ExposeCodeInResponse is true (development).</param>
public sealed record RequestOtpResponse(string Destination, int ExpiresInSeconds, string? DevCode);

public sealed record VerifyOtpRequest(string Identifier, string Code);

public sealed record ChangePasswordRequest(string CurrentPassword, string NewPassword);

public sealed record UserDto(Guid Id, UserRole Role, string Name, string? Email, string? Phone, Guid? ProfileId, bool IsActive);

public sealed record AuthResponse(string AccessToken, DateTime ExpiresAt, UserDto User);

public sealed record UpdateProfileRequest(string Name, string? Email, string? Phone);

// ═════════════════════════════════════════════════════════════════════════════
// Address (Google Places + normalized fields)
// ═════════════════════════════════════════════════════════════════════════════

public sealed record AddressDto(
    Guid Id,
    string? GooglePlaceId,
    string FormattedAddress,
    string? AddressLine1,
    string? AddressLine2,
    string? StreetNumber,
    string? Route,
    string? SubLocality,
    string? Locality,
    string? AdministrativeAreaLevel1,
    string? AdministrativeAreaLevel2,
    string? Country,
    string? CountryCode,
    string? PostalCode,
    decimal Latitude,
    decimal Longitude,
    string? PlusCode);

public sealed record UpsertAddressRequest(
    string? GooglePlaceId,
    string FormattedAddress,
    string? AddressLine1,
    string? AddressLine2,
    string? StreetNumber,
    string? Route,
    string? SubLocality,
    string? Locality,
    string? AdministrativeAreaLevel1,
    string? AdministrativeAreaLevel2,
    string? Country,
    string? CountryCode,
    string? PostalCode,
    decimal Latitude,
    decimal Longitude,
    string? PlusCode);

// ═════════════════════════════════════════════════════════════════════════════
// Categories & catalog
// ═════════════════════════════════════════════════════════════════════════════

public sealed record CategoryDto(Guid Id, string Name, string? Description, string? ImageUrl, int DisplayOrder, bool IsActive);

public sealed record CategoryRequest(string Name, string? Description, string? ImageUrl, int DisplayOrder, bool IsActive = true);

/// <param name="CategoryIds">All category ids in the desired display order.</param>
public sealed record ReorderRequest(IReadOnlyList<Guid> CategoryIds);

public sealed record SetActiveRequest(bool IsActive);

/// <param name="FastestDays">Shortest EstimatedDays among the provider's active items (in the requested category, if any).</param>
public sealed record ProviderSummaryDto(
    Guid Id, string BusinessName, string? Description, string? LogoUrl,
    string? Locality, IReadOnlyList<string> Categories, decimal? StartingPrice, int? FastestDays);

/// <summary>C05 – "Popular right now" on the customer home screen.</summary>
public sealed record PopularServiceItemDto(
    Guid ServiceItemId, Guid ProductId, Guid ProviderId, string ProviderName, Guid CategoryId, string CategoryName,
    string ProductName, string ServiceItemName, decimal Price, int EstimatedDays, int OrderedQuantity);

public sealed record ServiceItemDto(
    Guid Id, Guid ProductId, string Name, string? Description, string? ImageUrl,
    decimal Price, int EstimatedDays, int DisplayOrder, bool IsActive);

public sealed record ProductDto(
    Guid Id, Guid CategoryId, string CategoryName, string Name, string? Description, string? ImageUrl,
    int DisplayOrder, bool IsActive, decimal? StartingPrice, IReadOnlyList<ServiceItemDto> ServiceItems);

public sealed record CatalogCategoryDto(Guid Id, string Name, string? Description, string? ImageUrl, IReadOnlyList<ProductDto> Products);

public sealed record ProviderCatalogDto(Guid ProviderId, string BusinessName, string Currency, IReadOnlyList<CatalogCategoryDto> Categories);

public sealed record ProductRequest(Guid CategoryId, string Name, string? Description, string? ImageUrl, int? DisplayOrder, bool IsActive = true);

public sealed record ServiceItemRequest(string Name, string? Description, string? ImageUrl, decimal Price, int EstimatedDays, int? DisplayOrder, bool IsActive = true);

public sealed record ProviderCategoryDto(Guid CategoryId, string Name, bool IsEnabled);

public sealed record SetProviderCategoriesRequest(IReadOnlyList<Guid> CategoryIds);

public sealed record ProviderProfileDto(
    Guid Id, string BusinessName, string? Description, string? Phone, string? Email, string? LogoUrl,
    bool IsActive, UserDto Owner, AddressDto? Address, IReadOnlyList<CategoryDto> Categories);

public sealed record UpdateProviderProfileRequest(string BusinessName, string? Description, string? Phone, string? Email, string? LogoUrl);

// ═════════════════════════════════════════════════════════════════════════════
// Orders
// ═════════════════════════════════════════════════════════════════════════════

public sealed record CreateOrderItemRequest(Guid ServiceItemId, int Quantity, string? Notes = null);

/// <summary>Pickup address is the customer's current address (V1: one address per role).</summary>
/// <param name="PickupDate">Requested pickup date (today or later).</param>
/// <param name="PickupSlot">Requested pickup window label, e.g. "09:00 AM – 11:00 AM".</param>
public sealed record CreateOrderRequest(
    Guid ServiceProviderId,
    IReadOnlyList<CreateOrderItemRequest> Items,
    DateOnly? PickupDate = null,
    string? PickupSlot = null);

public sealed record OrderQuoteDto(decimal SubTotal, decimal Tax, decimal DeliveryFee, decimal TotalAmount, string Currency);

public sealed record PartyDto(Guid Id, string Name, string? Phone);

public sealed record OrderItemDto(
    Guid Id, Guid ProductId, Guid ServiceItemId, string CategoryName, string ProductName, string ServiceItemName,
    int Quantity, decimal UnitPrice, decimal TotalPrice, string? Notes, int EstimatedDays);

public sealed record StatusHistoryDto(OrderStatus Status, string? Notes, Guid? ChangedByUserId, DateTime CreatedAt);

public sealed record OrderPhotoDto(Guid Id, PhotoType Type, string Url, UploadedBy UploadedBy, DateTime CreatedAt);

public sealed record PaymentDto(Guid Id, decimal Amount, PaymentStatus Status, PaymentMethod Method, string? TransactionId, DateTime? PaidAt, DateTime CreatedAt);

public sealed record DisputeDto(
    Guid Id, Guid OrderId, string OrderNumber, Guid RaisedByUserId, string RaisedByName,
    string Reason, string? Description, DisputeStatus Status, string? Resolution, DateTime CreatedAt);

public sealed record DriverJobDto(
    Guid Id, Guid OrderId, string OrderNumber, DriverJobType JobType, DriverJobStatus Status,
    Guid? DriverId, string? DriverName, string? DriverPhone,
    string PickupName, AddressDto PickupAddress, string DeliveryName, AddressDto DeliveryAddress,
    int ItemCount, decimal EstimatedEarnings, DateTime? AssignedAt, DateTime? CompletedAt, DateTime CreatedAt,
    OrderStatus OrderStatus, DateTime? PickupDate, string? PickupSlot, bool IsOnHold);

/// <summary>The driver's next step on a job (derived from job type + status).</summary>
public enum DriverJobStep
{
    /// <summary>Pending on the marketplace.</summary>
    Accept,
    /// <summary>Pickup · Assigned – go to the customer, verify items, enter the pickup code.</summary>
    CollectFromCustomer,
    /// <summary>Pickup · InProgress – take the items to the provider and hand over.</summary>
    DropAtProvider,
    /// <summary>Delivery · Assigned – collect the packed order from the provider.</summary>
    CollectFromProvider,
    /// <summary>Delivery · InProgress – deliver with the customer's OTP.</summary>
    DeliverToCustomer,
    Done,
    /// <summary>Cancelled, taken by another driver, or the order is on hold.</summary>
    Unavailable
}

/// <summary>D03 – a driver job with everything the job screens need.</summary>
/// <param name="CustomerPhone">Only shared while the job is the caller's own and active.</param>
/// <param name="ProviderPhone">Only shared while the job is the caller's own and active.</param>
public sealed record DriverJobDetailDto(
    DriverJobDto Job,
    DriverJobStep NextStep,
    bool IsMine,
    string CustomerName,
    string? CustomerPhone,
    string ProviderName,
    string? ProviderPhone,
    IReadOnlyList<OrderItemDto> Items);

public sealed record OrderSummaryDto(
    Guid Id, string OrderNumber, OrderStatus Status, string CustomerName, string ProviderName,
    int ItemCount, string ItemsText, decimal TotalAmount, bool IsPaid, bool HasOpenDispute,
    DateTime? PickupDate, string? PickupSlot,
    DateTime CreatedAt, DateTime UpdatedAt);

public sealed record OrderDetailDto(
    Guid Id,
    string OrderNumber,
    OrderStatus Status,
    PartyDto Customer,
    PartyDto Provider,
    AddressDto PickupAddress,
    AddressDto? ProviderAddress,
    DateTime? PickupDate,
    string? PickupSlot,
    IReadOnlyList<OrderItemDto> Items,
    decimal SubTotal,
    decimal Tax,
    decimal DeliveryFee,
    decimal TotalAmount,
    string Currency,
    bool IsPaid,
    IReadOnlyList<StatusHistoryDto> History,
    IReadOnlyList<OrderPhotoDto> Photos,
    IReadOnlyList<DriverJobDto> DriverJobs,
    IReadOnlyList<PaymentDto> Payments,
    IReadOnlyList<DisputeDto> Disputes,
    string? PickupCode,
    string? DeliveryOtp,
    DateTime CreatedAt,
    DateTime UpdatedAt);

public enum ProviderOrderFilter
{
    /// <summary>Created (and paid) – waiting for Accept / Reject.</summary>
    New,
    /// <summary>Confirmed, PickupAssigned, PickedUp.</summary>
    Incoming,
    /// <summary>ReceivedByProvider, Processing, QualityCheck.</summary>
    InProcess,
    /// <summary>ReadyForDelivery, DeliveryAssigned, OutForDelivery.</summary>
    Ready,
    /// <summary>Delivered, Completed.</summary>
    Completed,
    Cancelled,
    Disputed,
    All
}

public enum CustomerOrderFilter
{
    Active,
    Completed,
    Cancelled,
    All
}

public sealed record CustomerOrderCountsDto(int Active, int Completed, int Cancelled);

public sealed record ProviderOrderCountsDto(int New, int Incoming, int InProcess, int Ready, int Completed, int Cancelled, int Disputed);

public sealed class ProviderOrdersQuery : Common.PagedRequest
{
    public ProviderOrderFilter Filter { get; set; } = ProviderOrderFilter.All;
}

public sealed class CustomerOrdersQuery : Common.PagedRequest
{
    public CustomerOrderFilter Filter { get; set; } = CustomerOrderFilter.All;
}

public sealed record ActivityDto(Guid OrderId, string OrderNumber, OrderStatus Status, string? Notes, DateTime At);

public sealed record ProviderDashboardDto(
    string BusinessName, int NewCount, int IncomingCount, int InProcessCount, int ReadyCount, int CompletedCount,
    int UnreadNotifications, IReadOnlyList<OrderSummaryDto> TodaysOrders, IReadOnlyList<ActivityDto> RecentActivity);

public sealed record ReasonRequest(string Reason);

public sealed record IssueRequest(string Reason, string? Description);

public sealed record ReceiveOrderRequest(int ActualItemCount, string? Notes);

public enum ItemCondition
{
    Good,
    Stained,
    Damaged
}

public sealed record InspectionItemRequest(Guid OrderItemId, int ReceivedQuantity, ItemCondition Condition, string? Note);

public sealed record InspectionRequest(IReadOnlyList<InspectionItemRequest> Items);

// ═════════════════════════════════════════════════════════════════════════════
// Driver
// ═════════════════════════════════════════════════════════════════════════════

public sealed record DriverDashboardDto(
    string DriverName, bool IsAvailable, int AvailableJobs, DriverJobDetailDto? ActiveJob, int CompletedToday, decimal EarningsToday,
    int UnreadNotifications, IReadOnlyList<DriverJobDto> TodaysJobs, string Currency);

public sealed record DriverJobCountsDto(int Available, int Mine, int Completed);

/// <summary>D05 – what the driver counted at the customer's door for one order line.</summary>
public sealed record DriverItemCheckRequest(Guid OrderItemId, int CountedQuantity, ItemCondition Condition, string? Note);

/// <summary>D06 – pickup code plus the item check. <c>ItemCount</c> is kept for older clients.</summary>
public sealed record DriverPickupRequest(string Code, IReadOnlyList<DriverItemCheckRequest>? Items = null, int? ItemCount = null, string? Notes = null);

/// <summary>D09 – collected from the provider.</summary>
public sealed record StartDeliveryRequest(int? PackageCount = null, string? Notes = null);

public sealed record DriverProfileDto(Guid Id, UserDto User, string LicenseNumber, bool IsAvailable, bool IsActive, AddressDto? Address);

/// <summary>Licence number is required; name, email and phone are updated when supplied.</summary>
public sealed record UpdateDriverProfileRequest(string LicenseNumber, string? Name = null, string? Email = null, string? Phone = null);

public enum EarningsPeriod
{
    /// <summary>Last 7 days, one bar per day.</summary>
    Week,
    /// <summary>Last 4 weeks, one bar per week.</summary>
    Month
}

public sealed record EarningsBarDto(string Label, DateTime Start, decimal Amount, int Jobs);

/// <summary>D13 – earnings are completed jobs × Marketplace:DriverEarningPerJob (no payouts table in V1).</summary>
public sealed record DriverEarningsDto(
    EarningsPeriod Period, DateTime From, DateTime To, decimal Total, int JobCount, int PickupCount, int DeliveryCount,
    decimal RatePerJob, decimal AveragePerJob, string Currency, IReadOnlyList<EarningsBarDto> Bars, IReadOnlyList<DriverJobDto> Jobs);

/// <param name="Code">Pickup code shown to the customer (QR) or delivery OTP.</param>
public sealed record VerifyCodeRequest(string Code, int? ItemCount, string? Notes);

public sealed class DriverJobsQuery : Common.PagedRequest
{
    /// <summary>Optional filter: Completed or Cancelled for history.</summary>
    public DriverJobStatus? Status { get; set; }
}

// ═════════════════════════════════════════════════════════════════════════════
// Payments (Paystack)
// ═════════════════════════════════════════════════════════════════════════════

public sealed record InitializePaymentResponse(
    Guid PaymentId, string Reference, string AuthorizationUrl, string AccessCode,
    decimal Amount, string Currency, string PublicKey);

// ═════════════════════════════════════════════════════════════════════════════
// Notifications
// ═════════════════════════════════════════════════════════════════════════════

public sealed record NotificationDto(Guid Id, Guid? OrderId, string Title, string Message, NotificationType Type, bool IsRead, DateTime CreatedAt);

// ═════════════════════════════════════════════════════════════════════════════
// Admin
// ═════════════════════════════════════════════════════════════════════════════

public sealed record DailyCountDto(DateOnly Date, int Orders, decimal Revenue);

public sealed record AdminDashboardDto(
    int TotalCustomers, int TotalProviders, int TotalDrivers, int ActiveOrders, int CompletedOrders,
    decimal Revenue, int OpenDisputes, string Currency,
    IReadOnlyDictionary<string, int> OrdersByStatus, IReadOnlyList<DailyCountDto> OrdersLast30Days);

public sealed record AdminCustomerDto(Guid Id, Guid UserId, string Name, string? Phone, string? Email, string? Address, int OrderCount, bool IsActive, DateTime CreatedAt);

public sealed record AdminProviderDto(Guid Id, Guid UserId, string OwnerName, string BusinessName, IReadOnlyList<string> Categories, int OrderCount, bool IsActive, DateTime CreatedAt);

/// <remarks>No ServiceProviderId – drivers are shared marketplace drivers.</remarks>
public sealed record AdminDriverDto(Guid Id, Guid UserId, string Name, string? Phone, string LicenseNumber, bool IsAvailable, int ActiveJobs, int CompletedJobs, bool IsActive);

public sealed record AdminOrderDto(
    Guid Id, string OrderNumber, string CustomerName, string ProviderName, string? DriverName,
    OrderStatus Status, decimal TotalAmount, DateTime CreatedAt);

public sealed record AdminPaymentDto(
    Guid Id, Guid OrderId, string OrderNumber, string CustomerName, decimal Amount,
    PaymentMethod Method, string? TransactionId, PaymentStatus Status, DateTime? PaidAt, DateTime CreatedAt);

public sealed record AssignDriverRequest(Guid DriverId);

/// <param name="Status">Resolved or Rejected.</param>
/// <param name="OrderStatus">Optional: status to move the order to (e.g. Processing to resume, Cancelled, Completed).</param>
public sealed record ResolveDisputeRequest(DisputeStatus Status, string Resolution, OrderStatus? OrderStatus);

public sealed class AdminOrdersQuery : Common.PagedRequest
{
    public OrderStatus? Status { get; set; }
}

public sealed class AdminJobsQuery : Common.PagedRequest
{
    public DriverJobStatus? Status { get; set; }
    public DriverJobType? JobType { get; set; }
}

public sealed class AdminDisputesQuery : Common.PagedRequest
{
    public DisputeStatus? Status { get; set; }
}
