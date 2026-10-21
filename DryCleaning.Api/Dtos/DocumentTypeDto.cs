using DryCleaning.Api.Domain;

namespace DryCleaning.Api.Dtos;

public sealed class DocumentTypeDto
{
    public required Guid Id { get; init; }
    public required UserRole Role { get; init; }
    public required string Name { get; init; }
    public required string? Description { get; init; }
    public required bool IsRequired { get; init; }
    public required string? NumberLabel { get; init; }
    public required string? NumberPattern { get; init; }
    public required bool HasExpiry { get; init; }
    public required bool IsActive { get; init; }
    public required int DisplayOrder { get; init; }
}
