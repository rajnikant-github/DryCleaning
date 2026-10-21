namespace DryCleaning.Services.Api.Contracts;

/// <summary>POST api/customer/orders/quote answer.</summary>
public sealed class OrderQuoteDto
{
    public decimal SubTotal { get; set; }
    public decimal Tax { get; set; }
    public decimal DeliveryFee { get; set; }
    public List<OrderExtraChargeDto> ExtraCharges { get; set; } = [];
    public decimal TotalAmount { get; set; }
    public decimal? DistanceKm { get; set; }
    public bool DistanceIsEstimate { get; set; }
    public int? DurationMinutes { get; set; }
}
