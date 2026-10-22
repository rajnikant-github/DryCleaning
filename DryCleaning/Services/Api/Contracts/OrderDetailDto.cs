using DryCleaning.Models;

namespace DryCleaning.Services.Api.Contracts;

/// <summary>Full order.</summary>
public sealed class OrderDetailDto
{
    public Guid Id { get; set; }
    public string OrderNumber { get; set; } = string.Empty;
    public OrderStatus Status { get; set; }
    public PartyDto Customer { get; set; } = new();
    public PartyDto Provider { get; set; } = new();
    public AddressDto PickupAddress { get; set; } = new();
    public AddressDto? ProviderAddress { get; set; }
    public DateTime? PickupDate { get; set; }
    public string? PickupSlot { get; set; }
    public string? DriverNotes { get; set; }
    public List<OrderItemDto> Items { get; set; } = [];
    public decimal SubTotal { get; set; }
    public decimal Tax { get; set; }
    public decimal VatRate { get; set; }
    public decimal DeliveryFee { get; set; }
    public List<OrderExtraChargeDto> ExtraCharges { get; set; } = [];
    public decimal TotalAmount { get; set; }
    public string Currency { get; set; } = string.Empty;
    public bool IsPaid { get; set; }
    public bool DriverHandedOver { get; set; }
    public RatingDto? Rating { get; set; }
    public bool CanRate { get; set; }
    public List<StatusHistoryDto> History { get; set; } = [];
    public List<OrderPhotoDto> Photos { get; set; } = [];
    public List<DriverJobDto> DriverJobs { get; set; } = [];
    public List<PaymentDto> Payments { get; set; } = [];
    public List<DisputeDto> Disputes { get; set; } = [];
    public string? PickupCode { get; set; }
    public string? DeliveryOtp { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}
