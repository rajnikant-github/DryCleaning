using DryCleaning.Models;

namespace DryCleaning.Services.Api.Contracts;

/// <summary>Product with its service items.</summary>
public sealed class ProductDto
{
    public Guid Id { get; set; }
    public Guid ServiceProviderId { get; set; }
    public Guid CategoryId { get; set; }
    public string CategoryName { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string? ImageUrl { get; set; }
    public int DisplayOrder { get; set; }
    public bool IsActive { get; set; }
    public decimal? StartingPrice { get; set; }
    public List<ServiceItemDto> ServiceItems { get; set; } = [];
}
