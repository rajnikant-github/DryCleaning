namespace DryCleaning.Api.Dtos;

public sealed class PricePreviewDto
{
    public required decimal SubTotal { get; init; }
    public required decimal DeliveryFee { get; init; }
    public required IReadOnlyList<OrderExtraChargeDto> ExtraCharges { get; init; }
    public required decimal Tax { get; init; }
    public required decimal VatRate { get; init; }
    public required decimal TotalAmount { get; init; }
    public required string Currency { get; init; }
}
