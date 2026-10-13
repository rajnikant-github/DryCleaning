using DryCleaning.Api.Domain;

namespace DryCleaning.Api.Dtos;

public sealed class ServiceItemRequest
{
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string? ImageUrl { get; set; }
    public decimal Price { get; set; }
    public int EstimatedDays { get; set; }
    public int? DisplayOrder { get; set; }
    public bool IsActive { get; set; } = true;
}
