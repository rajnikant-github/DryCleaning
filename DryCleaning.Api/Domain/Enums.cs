namespace DryCleaning.Api.Domain;

// Values mirror the V1 schema. Stored as strings in SQL Server.

public enum UserRole
{
    Customer,
    Provider,
    Driver,
    Admin
}

public enum OrderStatus
{
    Created,
    Confirmed,
    PickupAssigned,
    PickedUp,
    ReceivedByProvider,
    Processing,
    QualityCheck,
    ReadyForDelivery,
    DeliveryAssigned,
    OutForDelivery,
    Delivered,
    Completed,
    Cancelled,
    Disputed
}

public enum VerificationType
{
    OTP,
    QR,
    Signature,
    Item
}

public enum PaymentStatus
{
    Pending,
    Success,
    Failed
}

public enum PaymentMethod
{
    Card,
    UPI,
    COD,
    Wallet
}

public enum PhotoType
{
    Pickup,
    Inspection,
    Completion,
    Damage
}

public enum UploadedBy
{
    Customer,
    Provider,
    Driver
}

public enum DriverJobType
{
    Pickup,
    Delivery
}

public enum DriverJobStatus
{
    /// <summary>Published to the marketplace, no driver yet.</summary>
    Pending,
    /// <summary>A driver accepted the job.</summary>
    Assigned,
    /// <summary>Driver has the items (on the way to destination).</summary>
    InProgress,
    Completed,
    Cancelled
}

public enum DisputeStatus
{
    Open,
    UnderReview,
    Resolved,
    Rejected
}

public enum NotificationType
{
    Order,
    Payment,
    Promo,
    System
}
