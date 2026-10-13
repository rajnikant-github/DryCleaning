using DryCleaning.Models;

namespace DryCleaning.Services.Api.Contracts;

/// <summary>Category section of a provider catalog.</summary>
public sealed class CatalogCategoryDto
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string? ImageUrl { get; set; }
    public List<ProductDto> Products { get; set; } = [];
}
