using DryCleaning.Api.Domain;

namespace DryCleaning.Api.Dtos;

public sealed class OrderQuoteDto
{
    public required decimal SubTotal { get; init; }
    public required decimal Tax { get; init; }
    public required decimal DeliveryFee { get; init; }
    public required decimal TotalAmount { get; init; }
    public required string Currency { get; init; }
}
