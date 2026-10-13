using DryCleaning.Api.Domain;

namespace DryCleaning.Api.Dtos;

public sealed class ServiceItemDto
{
    public required Guid Id { get; init; }
    public required Guid ProductId { get; init; }
    public required string Name { get; init; }
    public required string? Description { get; init; }
    public required string? ImageUrl { get; init; }
    public required decimal Price { get; init; }
    public required int EstimatedDays { get; init; }
    public required int DisplayOrder { get; init; }
    public required bool IsActive { get; init; }
}
