using DryCleaning.Api.Domain;

namespace DryCleaning.Api.Dtos;

public sealed class ProviderCategoryDto
{
    public required Guid CategoryId { get; init; }
    public required string Name { get; init; }
    public required bool IsEnabled { get; init; }
}
