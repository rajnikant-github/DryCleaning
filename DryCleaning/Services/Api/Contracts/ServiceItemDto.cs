using DryCleaning.Models;

namespace DryCleaning.Services.Api.Contracts;

/// <summary>Service item (price line).</summary>
public sealed class ServiceItemDto
{
    public Guid Id { get; set; }
    public Guid ProductId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string? ImageUrl { get; set; }
    public decimal Price { get; set; }
    public int EstimatedDays { get; set; }
    public int DisplayOrder { get; set; }
    public bool IsActive { get; set; }
}
