namespace DryCleaning.Admin.Models;

public sealed class VatSettingDto
{
    public decimal Rate { get; set; }
    public bool IsEnabled { get; set; }
    public bool AppliesToDelivery { get; set; }
}
