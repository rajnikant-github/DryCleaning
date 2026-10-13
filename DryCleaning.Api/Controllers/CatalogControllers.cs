using DryCleaning.Api.Common;
using DryCleaning.Api.Dtos;
using DryCleaning.Api.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace DryCleaning.Api.Controllers;

/// <summary>Public catalog: Category → Provider → Product → Service Item.</summary>
[ApiController]
[Route("api")]
[AllowAnonymous]
[Produces("application/json")]
public sealed class CatalogController(CategoryService categories, PublicCatalogService catalog) : ControllerBase
{
    /// <summary>Active categories (Dry Cleaning, Shoe Cleaning, …).</summary>
    [HttpGet("categories")]
    public Task<IReadOnlyList<CategoryDto>> GetCategories(CancellationToken ct) => categories.GetAsync(includeInactive: false, ct);

    [HttpGet("categories/{id:guid}")]
    public Task<CategoryDto> GetCategory(Guid id, CancellationToken ct) => categories.GetByIdAsync(id, ct);

    /// <summary>Approved providers, optionally filtered by category.</summary>
    [HttpGet("providers")]
    public Task<PagedResult<ProviderSummaryDto>> GetProviders([FromQuery] Guid? categoryId, [FromQuery] PagedRequest request, CancellationToken ct) =>
        catalog.GetProvidersAsync(categoryId, request, ct);

    /// <summary>A provider's active catalog (categories → products → service items).</summary>
    [HttpGet("providers/{providerId:guid}/catalog")]
    public Task<ProviderCatalogDto> GetProviderCatalog(Guid providerId, CancellationToken ct) =>
        catalog.GetProviderCatalogAsync(providerId, ct);
}
