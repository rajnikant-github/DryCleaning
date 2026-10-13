using DryCleaning.Api.Domain;

namespace DryCleaning.Api.Dtos;

public sealed class SetProviderCategoriesRequest
{
    public IReadOnlyList<Guid> CategoryIds { get; set; } = [];
}
