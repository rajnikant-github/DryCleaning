namespace DryCleaning.Admin.Models;

public sealed class ExtraChargeDto
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public decimal Amount { get; set; }
    public bool IsActive { get; set; }
    public int DisplayOrder { get; set; }
}
