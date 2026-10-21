namespace DryCleaning.Api.Dtos;

/// <summary>A sample order to price with the current settings (admin preview).</summary>
public sealed class PricePreviewRequest
{
    public decimal ItemsSubTotal { get; set; }
    public decimal DeliveryFee { get; set; }
}
