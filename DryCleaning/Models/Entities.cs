namespace DryCleaning.Models;

// One class per table in the V1 schema. Kept as plain POCOs so they can be
// reused as API contracts when the mock services are replaced.

public abstract class EntityBase
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public DateTime CreatedAt { get; set; } = DateTime.Now;
    public DateTime UpdatedAt { get; set; } = DateTime.Now;
}

// ── Users & roles ────────────────────────────────────────────────────────────

public sealed class User : EntityBase
{
    public UserRole Role { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string Phone { get; set; } = string.Empty;
    public string PasswordHash { get; set; } = string.Empty;
    public bool IsActive { get; set; } = true;
}

public sealed class Customer : EntityBase
{
    public Guid UserId { get; set; }
    public Guid AddressId { get; set; }
}

/// <summary>Maps to the <c>ServiceProvider</c> table (renamed to avoid clashing with DI's ServiceProvider).</summary>
public sealed class Provider : EntityBase
{
    public Guid UserId { get; set; }
    public Guid AddressId { get; set; }
    public string BusinessName { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string Phone { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string? LogoUrl { get; set; }
    public bool IsActive { get; set; } = true;
}

/// <summary>Shared marketplace driver. Intentionally has NO ServiceProviderId.</summary>
public sealed class Driver : EntityBase
{
    public Guid UserId { get; set; }
    public Guid AddressId { get; set; }
    public string LicenseNumber { get; set; } = string.Empty;
    public bool IsAvailable { get; set; } = true;
    public bool IsActive { get; set; } = true;
}

// ── Address (Google Places + normalized fields) ──────────────────────────────

public sealed class Address : EntityBase
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

// ── Catalog: Provider ↔ Category → Product → ServiceItem ─────────────────────

public sealed class Category : EntityBase
{
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string? ImageUrl { get; set; }
    public int DisplayOrder { get; set; }
    public bool IsActive { get; set; } = true;
}

public sealed class ServiceProviderCategory : EntityBase
{
    public Guid ServiceProviderId { get; set; }
    public Guid CategoryId { get; set; }
    public bool IsActive { get; set; } = true;
}

public sealed class Product : EntityBase
{
    public Guid ServiceProviderId { get; set; }
    public Guid CategoryId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string? ImageUrl { get; set; }
    public int DisplayOrder { get; set; }
    public bool IsActive { get; set; } = true;
}

public sealed class ServiceItem : EntityBase
{
    public Guid ProductId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string? ImageUrl { get; set; }
    public decimal Price { get; set; }
    public int EstimatedDays { get; set; }
    public int DisplayOrder { get; set; }
    public bool IsActive { get; set; } = true;
}

// ── Orders & fulfilment ──────────────────────────────────────────────────────

public sealed class Order : EntityBase
{
    public string OrderNumber { get; set; } = string.Empty;
    public Guid CustomerId { get; set; }
    public Guid ServiceProviderId { get; set; }
    public Guid PickupAddressId { get; set; }
    public OrderStatus Status { get; set; }
    public decimal SubTotal { get; set; }
    public decimal Tax { get; set; }
    public decimal DeliveryFee { get; set; }
    public decimal TotalAmount { get; set; }

    /// <summary>Requested pickup date (added to schema: Orders.PickupDate).</summary>
    public DateTime? PickupDate { get; set; }

    /// <summary>Requested pickup window, e.g. "09:00 – 11:00" (added to schema: Orders.PickupSlot).</summary>
    public string? PickupSlot { get; set; }
}

public sealed class OrderItem : EntityBase
{
    public Guid OrderId { get; set; }
    public Guid ServiceItemId { get; set; }
    public Guid ProductId { get; set; }
    public int Quantity { get; set; }
    public decimal UnitPrice { get; set; }
    public decimal TotalPrice { get; set; }

    /// <summary>Customer's special instructions (added to schema: OrderItems.Notes).</summary>
    public string? Notes { get; set; }
}

public sealed class OrderStatusHistory : EntityBase
{
    public Guid OrderId { get; set; }
    public OrderStatus Status { get; set; }
    public Guid? ChangedByUserId { get; set; }
    public string? Notes { get; set; }
}

public sealed class OrderVerification : EntityBase
{
    public Guid OrderId { get; set; }
    public VerificationType Type { get; set; }
    public string? OtpCode { get; set; }
    public DateTime? VerifiedAt { get; set; }
    public Guid? VerifiedByUserId { get; set; }
}

public sealed class OrderPhoto : EntityBase
{
    public Guid OrderId { get; set; }
    public PhotoType Type { get; set; }
    public string Url { get; set; } = string.Empty;
    public UploadedBy UploadedBy { get; set; }
}

public sealed class Payment : EntityBase
{
    public Guid OrderId { get; set; }
    public decimal Amount { get; set; }
    public PaymentStatus Status { get; set; }
    public PaymentMethod Method { get; set; }
    public string? TransactionId { get; set; }
    public DateTime? PaidAt { get; set; }
}

public sealed class DriverJob : EntityBase
{
    public Guid OrderId { get; set; }
    public Guid DriverId { get; set; }
    public DriverJobType JobType { get; set; }
    public DriverJobStatus Status { get; set; }
    public Guid PickupAddressId { get; set; }
    public Guid DeliveryAddressId { get; set; }
    public DateTime? AssignedAt { get; set; }
    public DateTime? CompletedAt { get; set; }
}

public sealed class Dispute : EntityBase
{
    public Guid OrderId { get; set; }
    public Guid RaisedByUserId { get; set; }
    public string Reason { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public DisputeStatus Status { get; set; }
    public string? Resolution { get; set; }
}

public sealed class Notification : EntityBase
{
    public Guid UserId { get; set; }
    public Guid? OrderId { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
    public NotificationType Type { get; set; }
    public bool IsRead { get; set; }
}
