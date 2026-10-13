using DryCleaning.Api.Domain;

namespace DryCleaning.Api.Dtos;

// Categories & catalog

public sealed class CategoryDto
{
    public required Guid Id { get; init; }
    public required string Name { get; init; }
    public required string? Description { get; init; }
    public required string? ImageUrl { get; init; }
    public required int DisplayOrder { get; init; }
    public required bool IsActive { get; init; }
}
