using DryCleaning.Models;

namespace DryCleaning.Services.Api.Contracts;

/// <summary>GET api/providers/{id}/catalog.</summary>
public sealed class ProviderCatalogDto
{
    public Guid ProviderId { get; set; }
    public string BusinessName { get; set; } = string.Empty;
    public string Currency { get; set; } = string.Empty;
    public List<CatalogCategoryDto> Categories { get; set; } = [];
}
