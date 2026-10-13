namespace DryCleaning.Models;

public sealed class ServiceItem : EntityBase
{
    public Guid ProductId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string? ImageUrl { get; set; }
    public decimal Price { get; set; }
    public int EstimatedDays { get; set; }
    public int DisplayOrder { get; set; }
    public bool IsActive { get; set; } = true;
}
