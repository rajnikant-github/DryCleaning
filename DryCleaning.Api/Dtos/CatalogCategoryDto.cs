using DryCleaning.Api.Domain;

namespace DryCleaning.Api.Dtos;

public sealed class CatalogCategoryDto
{
    public required Guid Id { get; init; }
    public required string Name { get; init; }
    public required string? Description { get; init; }
    public required string? ImageUrl { get; init; }
    public required IReadOnlyList<ProductDto> Products { get; init; }
}
