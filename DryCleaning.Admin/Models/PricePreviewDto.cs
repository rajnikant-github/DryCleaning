namespace DryCleaning.Admin.Models;

public sealed class PricePreviewDto
{
    public decimal SubTotal { get; set; }
    public decimal DeliveryFee { get; set; }
    public List<OrderExtraChargeDto> ExtraCharges { get; set; } = [];
    public decimal Tax { get; set; }
    public decimal VatRate { get; set; }
    public decimal TotalAmount { get; set; }
    public string Currency { get; set; } = "ZAR";
}
