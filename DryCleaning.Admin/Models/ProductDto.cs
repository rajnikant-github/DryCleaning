namespace DryCleaning.Admin.Models;

public sealed class ProductDto
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public bool IsActive { get; set; }
    public List<ServiceItemDto> ServiceItems { get; set; } = [];
}
