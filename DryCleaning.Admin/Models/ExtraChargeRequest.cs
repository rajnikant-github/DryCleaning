namespace DryCleaning.Admin.Models;

public sealed class ExtraChargeRequest
{
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public ExtraChargeType Type { get; set; } = ExtraChargeType.Fixed;
    public decimal Amount { get; set; }
    public decimal Percent { get; set; }
    public ChargeBase PercentBase { get; set; } = ChargeBase.ItemsSubtotal;
    public bool IsTaxable { get; set; }
    public bool IsActive { get; set; } = true;
    public int DisplayOrder { get; set; }
}
