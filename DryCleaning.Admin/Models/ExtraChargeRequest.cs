namespace DryCleaning.Admin.Models;

public sealed class ExtraChargeRequest
{
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public decimal Amount { get; set; }
    public bool IsActive { get; set; } = true;
    public int DisplayOrder { get; set; }
}
