namespace DryCleaning.Admin.Models;

public sealed class PricePreviewRequest
{
    public decimal ItemsSubTotal { get; set; }
    public decimal DeliveryFee { get; set; }
}
