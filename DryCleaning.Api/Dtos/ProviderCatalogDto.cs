using DryCleaning.Api.Domain;

namespace DryCleaning.Api.Dtos;

public sealed class ProviderCatalogDto
{
    public required Guid ProviderId { get; init; }
    public required string BusinessName { get; init; }
    public required string Currency { get; init; }
    public required IReadOnlyList<CatalogCategoryDto> Categories { get; init; }
}
