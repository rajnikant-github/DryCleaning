using DryCleaning.Api.Domain;

namespace DryCleaning.Api.Dtos;

public sealed class OrderQuoteDto
{
    public required decimal SubTotal { get; init; }
    public required decimal Tax { get; init; }

    /// <summary>The VAT percentage applied (0 when VAT is off), e.g. 15.</summary>
    public decimal VatRate { get; init; }
    public required decimal DeliveryFee { get; init; }

    /// <summary>Extra charges that would be added (already included in <see cref="TotalAmount"/>).</summary>
    public IReadOnlyList<OrderExtraChargeDto> ExtraCharges { get; init; } = [];
    public required decimal TotalAmount { get; init; }
    public required string Currency { get; init; }

    /// <summary>Estimated road distance used for the delivery fee; null when an address has no location.</summary>
    public decimal? DistanceKm { get; init; }

    /// <summary>True when the distance is a straight-line estimate (Google couldn't measure the road distance).</summary>
    public bool DistanceIsEstimate { get; init; }

    /// <summary>Estimated driving time in minutes (only when the distance was measured on the road).</summary>
    public int? DurationMinutes { get; init; }
}
