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
