using DryCleaning.Api.Domain;

namespace DryCleaning.Api.Dtos;

public sealed class OrderDetailDto
{
    public required Guid Id { get; init; }
    public required string OrderNumber { get; init; }
    public required OrderStatus Status { get; init; }
    public required PartyDto Customer { get; init; }
    public required PartyDto Provider { get; init; }
    public required AddressDto PickupAddress { get; init; }
    public required AddressDto? ProviderAddress { get; init; }
    public required DateTime? PickupDate { get; init; }
    public required string? PickupSlot { get; init; }

    /// <summary>The customer's note for the driver (leave at the gate, etc.).</summary>
    public required string? DriverNotes { get; init; }
    public required IReadOnlyList<OrderItemDto> Items { get; init; }
    public required decimal SubTotal { get; init; }
    public required decimal Tax { get; init; }

    /// <summary>The VAT percentage this order was charged (0 = no VAT), e.g. 15.</summary>
    public required decimal VatRate { get; init; }
    public required decimal DeliveryFee { get; init; }

    /// <summary>Extra charges the admin had switched on when the order was placed (already included in <see cref="TotalAmount"/>).</summary>
    public required IReadOnlyList<OrderExtraChargeDto> ExtraCharges { get; init; }
    public required decimal TotalAmount { get; init; }
    public required string Currency { get; init; }
    public required bool IsPaid { get; init; }

    /// <summary>True once the driver has handed the picked-up items over to the provider (the pickup job is completed).</summary>
    public required bool DriverHandedOver { get; init; }

    /// <summary>The customer's rating of the provider, once given.</summary>
    public required RatingDto? Rating { get; init; }

    /// <summary>True when the order is completed and not rated yet (customers only).</summary>
    public required bool CanRate { get; init; }
    public required IReadOnlyList<StatusHistoryDto> History { get; init; }
    public required IReadOnlyList<OrderPhotoDto> Photos { get; init; }
    public required IReadOnlyList<DriverJobDto> DriverJobs { get; init; }
    public required IReadOnlyList<PaymentDto> Payments { get; init; }
    public required IReadOnlyList<DisputeDto> Disputes { get; init; }
    public required string? PickupCode { get; init; }
    public required string? DeliveryOtp { get; init; }
    public required DateTime CreatedAt { get; init; }
    public required DateTime UpdatedAt { get; init; }
}
