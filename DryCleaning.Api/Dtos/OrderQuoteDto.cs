using DryCleaning.Api.Domain;

namespace DryCleaning.Api.Dtos;

public sealed class OrderQuoteDto
{
    public required decimal SubTotal { get; init; }
    public required decimal Tax { get; init; }
    public required decimal DeliveryFee { get; init; }
    public required decimal TotalAmount { get; init; }
    public required string Currency { get; init; }

    /// <summary>Estimated road distance used for the delivery fee; null when an address has no location.</summary>
    public decimal? DistanceKm { get; init; }
}
