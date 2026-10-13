using DryCleaning.Api.Domain;

namespace DryCleaning.Api.Dtos;

public sealed class ProductDto
{
    public required Guid Id { get; init; }
    public required Guid ServiceProviderId { get; init; }
    public required Guid CategoryId { get; init; }
    public required string CategoryName { get; init; }
    public required string Name { get; init; }
    public required string? Description { get; init; }
    public required string? ImageUrl { get; init; }
    public required int DisplayOrder { get; init; }
    public required bool IsActive { get; init; }
    public required decimal? StartingPrice { get; init; }
    public required IReadOnlyList<ServiceItemDto> ServiceItems { get; init; }
}
