namespace DryCleaning.Admin.Models;

public sealed class OrderExtraChargeDto
{
    public string Name { get; set; } = string.Empty;
    public decimal Amount { get; set; }
}
