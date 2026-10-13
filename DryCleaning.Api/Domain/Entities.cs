
namespace DryCleaning.Api.Domain;

// ── Users & roles ────────────────────────────────────────────────────────────

public class User : EntityBase
{
    public UserRole Role { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Email { get; set; }
    public string? Phone { get; set; }
    public string PasswordHash { get; set; } = string.Empty;
    public bool IsActive { get; set; } = true;

    public Customer? Customer { get; set; }
    public Provider? ServiceProvider { get; set; }
    public Driver? Driver { get; set; }
}

public class Customer : EntityBase
{
    public Guid UserId { get; set; }
    public Guid? AddressId { get; set; }

    public User User { get; set; } = null!;
    public Address? Address { get; set; }
    public ICollection<Order> Orders { get; set; } = [];
}

/// <summary>Maps to the ServiceProviders table (class named Provider to avoid clashing with DI's ServiceProvider).</summary>
public class Provider : EntityBase
{
    public Guid UserId { get; set; }
    public Guid? AddressId { get; set; }
    public string BusinessName { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string? Phone { get; set; }
    public string? Email { get; set; }
    public string? LogoUrl { get; set; }

    /// <summary>False until approved by an admin.</summary>
    public bool IsActive { get; set; }

    public User User { get; set; } = null!;
    public Address? Address { get; set; }
    public ICollection<ServiceProviderCategory> Categories { get; set; } = [];
    public ICollection<Product> Products { get; set; } = [];
    public ICollection<Order> Orders { get; set; } = [];
}

/// <summary>Shared marketplace driver. Intentionally has NO ServiceProviderId.</summary>
public class Driver : EntityBase
{
    public Guid UserId { get; set; }
    public Guid? AddressId { get; set; }
    public string LicenseNumber { get; set; } = string.Empty;
    public bool IsAvailable { get; set; }
    public bool IsActive { get; set; } = true;

    public User User { get; set; } = null!;
    public Address? Address { get; set; }
    public ICollection<DriverJob> Jobs { get; set; } = [];
}

// ── Address (Google Places + normalized fields) ──────────────────────────────

public class Address : EntityBase
{
    public string? GooglePlaceId { get; set; }
    public string FormattedAddress { get; set; } = string.Empty;
    public string? AddressLine1 { get; set; }
    public string? AddressLine2 { get; set; }
    public string? StreetNumber { get; set; }
    public string? Route { get; set; }
    public string? SubLocality { get; set; }
    public string? Locality { get; set; }
    public string? AdministrativeAreaLevel1 { get; set; }
    public string? AdministrativeAreaLevel2 { get; set; }
    public string? Country { get; set; }
    public string? CountryCode { get; set; }
    public string? PostalCode { get; set; }
    public decimal Latitude { get; set; }
    public decimal Longitude { get; set; }
    public string? PlusCode { get; set; }
}

// ── Catalog: ServiceProvider ↔ Category → Product → ServiceItem ─────────────

public class Category : EntityBase
{
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string? ImageUrl { get; set; }
    public int DisplayOrder { get; set; }
    public bool IsActive { get; set; } = true;

    public ICollection<Product> Products { get; set; } = [];
}

public class ServiceProviderCategory : EntityBase
{
    public Guid ServiceProviderId { get; set; }
    public Guid CategoryId { get; set; }
    public bool IsActive { get; set; } = true;

    public Provider ServiceProvider { get; set; } = null!;
    public Category Category { get; set; } = null!;
}

public class Product : EntityBase
{
    public Guid ServiceProviderId { get; set; }
    public Guid CategoryId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string? ImageUrl { get; set; }
    public int DisplayOrder { get; set; }
    public bool IsActive { get; set; } = true;

    public Provider ServiceProvider { get; set; } = null!;
    public Category Category { get; set; } = null!;
    public ICollection<ServiceItem> ServiceItems { get; set; } = [];
}

public class ServiceItem : EntityBase
{
    public Guid ProductId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string? ImageUrl { get; set; }
    public decimal Price { get; set; }
    public int EstimatedDays { get; set; }
    public int DisplayOrder { get; set; }
    public bool IsActive { get; set; } = true;

    public Product Product { get; set; } = null!;
}

// ── Orders & fulfilment ──────────────────────────────────────────────────────

public class Order : EntityBase
{
    public string OrderNumber { get; set; } = string.Empty;
    public Guid CustomerId { get; set; }
    public Guid ServiceProviderId { get; set; }
    public Guid PickupAddressId { get; set; }
    public OrderStatus Status { get; private set; } = OrderStatus.Created;
    public decimal SubTotal { get; set; }
    public decimal Tax { get; set; }
    public decimal DeliveryFee { get; set; }
    public decimal TotalAmount { get; set; }

    /// <summary>Requested pickup date (schema addition: Orders.PickupDate).</summary>
    public DateTime? PickupDate { get; set; }

    /// <summary>Requested pickup window, e.g. "09:00 AM – 11:00 AM" (schema addition: Orders.PickupSlot).</summary>
    public string? PickupSlot { get; set; }

    public Customer Customer { get; set; } = null!;
    public Provider ServiceProvider { get; set; } = null!;
    public Address PickupAddress { get; set; } = null!;
    public ICollection<OrderItem> Items { get; set; } = [];
    public ICollection<OrderStatusHistory> StatusHistory { get; set; } = [];
    public ICollection<OrderVerification> Verifications { get; set; } = [];
    public ICollection<OrderPhoto> Photos { get; set; } = [];
    public ICollection<Payment> Payments { get; set; } = [];
    public ICollection<DriverJob> DriverJobs { get; set; } = [];
    public ICollection<Dispute> Disputes { get; set; } = [];

    /// <summary>Moves the order to a new status (validated) and records history.</summary>
    public OrderStatusHistory ChangeStatus(OrderStatus to, Guid? changedByUserId, string? notes = null)
    {
        OrderWorkflow.EnsureCanTransition(Status, to);
        Status = to;
        Touch();
        var entry = new OrderStatusHistory
        {
            OrderId = Id,
            Status = to,
            ChangedByUserId = changedByUserId,
            Notes = notes
        };
        StatusHistory.Add(entry);
        return entry;
    }

    /// <summary>Admin-only override (e.g. resolving a dispute). Skips workflow validation but still records history.</summary>
    public OrderStatusHistory ForceStatus(OrderStatus to, Guid? changedByUserId, string notes)
    {
        Status = to;
        Touch();
        var entry = new OrderStatusHistory { OrderId = Id, Status = to, ChangedByUserId = changedByUserId, Notes = notes };
        StatusHistory.Add(entry);
        return entry;
    }

    /// <summary>Records the initial "Created" history row for a new order.</summary>
    public void MarkCreated(Guid? userId)
    {
        Status = OrderStatus.Created;
        StatusHistory.Add(new OrderStatusHistory { OrderId = Id, Status = OrderStatus.Created, ChangedByUserId = userId });
    }

    public void Recalculate(decimal vatRate, decimal deliveryFee)
    {
        SubTotal = Items.Sum(i => i.TotalPrice);
        DeliveryFee = deliveryFee;
        Tax = Math.Round(SubTotal * vatRate, 2, MidpointRounding.AwayFromZero);
        TotalAmount = SubTotal + Tax + DeliveryFee;
    }
}

public class OrderItem : EntityBase
{
    public Guid OrderId { get; set; }
    public Guid ServiceItemId { get; set; }
    public Guid ProductId { get; set; }
    public int Quantity { get; set; }
    public decimal UnitPrice { get; set; }
    public decimal TotalPrice { get; set; }

    /// <summary>Customer's special instructions (schema addition: OrderItems.Notes).</summary>
    public string? Notes { get; set; }

    public Order Order { get; set; } = null!;
    public ServiceItem ServiceItem { get; set; } = null!;
    public Product Product { get; set; } = null!;
}

public class OrderStatusHistory : EntityBase
{
    public Guid OrderId { get; set; }
    public OrderStatus Status { get; set; }
    public Guid? ChangedByUserId { get; set; }
    public string? Notes { get; set; }

    public Order Order { get; set; } = null!;
}

public class OrderVerification : EntityBase
{
    public Guid OrderId { get; set; }
    public VerificationType Type { get; set; }
    public string? OtpCode { get; set; }
    public DateTime? VerifiedAt { get; set; }
    public Guid? VerifiedByUserId { get; set; }

    public Order Order { get; set; } = null!;
}

public class OrderPhoto : EntityBase
{
    public Guid OrderId { get; set; }
    public PhotoType Type { get; set; }
    public string Url { get; set; } = string.Empty;
    public UploadedBy UploadedBy { get; set; }

    public Order Order { get; set; } = null!;
}

public class Payment : EntityBase
{
    public Guid OrderId { get; set; }
    public decimal Amount { get; set; }
    public PaymentStatus Status { get; set; } = PaymentStatus.Pending;
    public PaymentMethod Method { get; set; } = PaymentMethod.Card;

    /// <summary>Paystack transaction reference.</summary>
    public string? TransactionId { get; set; }
    public DateTime? PaidAt { get; set; }

    public Order Order { get; set; } = null!;
}

public class DriverJob : EntityBase
{
    public Guid OrderId { get; set; }

    /// <summary>Null while the job is Pending on the marketplace.</summary>
    public Guid? DriverId { get; set; }
    public DriverJobType JobType { get; set; }
    public DriverJobStatus Status { get; set; } = DriverJobStatus.Pending;
    public Guid PickupAddressId { get; set; }
    public Guid DeliveryAddressId { get; set; }
    public DateTime? AssignedAt { get; set; }
    public DateTime? CompletedAt { get; set; }

    public Order Order { get; set; } = null!;
    public Driver? Driver { get; set; }
    public Address PickupAddress { get; set; } = null!;
    public Address DeliveryAddress { get; set; } = null!;
}

public class Dispute : EntityBase
{
    public Guid OrderId { get; set; }
    public Guid RaisedByUserId { get; set; }
    public string Reason { get; set; } = string.Empty;
    public string? Description { get; set; }
    public DisputeStatus Status { get; set; } = DisputeStatus.Open;
    public string? Resolution { get; set; }

    public Order Order { get; set; } = null!;
    public User RaisedByUser { get; set; } = null!;
}

public class Notification : EntityBase
{
    public Guid UserId { get; set; }
    public Guid? OrderId { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
    public NotificationType Type { get; set; }
    public bool IsRead { get; set; }

    public User User { get; set; } = null!;
    public Order? Order { get; set; }
}

// ── Schema additions (not in the V1 diagram) ─────────────────────────────────

/// <summary>A phone registered for push notifications (Firebase Cloud Messaging token).</summary>
public class DeviceToken : EntityBase
{
    public Guid UserId { get; set; }
    public DevicePlatform Platform { get; set; }

    /// <summary>FCM registration token – unique across all users.</summary>
    public string Token { get; set; } = string.Empty;
    public DateTime LastSeenAt { get; set; } = DateTime.UtcNow;

    public User User { get; set; } = null!;
}

/// <summary>
/// Long-lived token used to get a new access token. Only a SHA-256 hash is stored.
/// Tokens rotate on every use; re-using a rotated token revokes all of the user's sessions.
/// </summary>
public class RefreshToken : EntityBase
{
    public Guid UserId { get; set; }
    public string TokenHash { get; set; } = string.Empty;
    public DateTime ExpiresAt { get; set; }
    public DateTime? RevokedAt { get; set; }
    public string? ReplacedByTokenHash { get; set; }

    public User User { get; set; } = null!;

    public bool IsActive => RevokedAt is null && ExpiresAt > DateTime.UtcNow;
}
