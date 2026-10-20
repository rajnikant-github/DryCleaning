namespace DryCleaning.Admin.Models;

public sealed class ServiceItemDto
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public decimal Price { get; set; }
    public int EstimatedDays { get; set; }
    public bool IsActive { get; set; }
}
