using DryCleaning.Api.Domain;

namespace DryCleaning.Api.Dtos;

public sealed class ProviderOrdersQuery : Common.PagedRequest
{
    public ProviderOrderFilter Filter { get; set; } = ProviderOrderFilter.All;
}
