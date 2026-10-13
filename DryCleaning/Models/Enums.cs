namespace DryCleaning.Models;

// Enum names/values mirror the V1 database schema so they serialize 1:1 with the API.

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
    Assigned,
    Accepted,
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
