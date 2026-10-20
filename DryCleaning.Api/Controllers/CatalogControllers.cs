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
public sealed class CatalogController(CategoryService categories, PublicCatalogService catalog, WorkingHoursService workingHours) : ControllerBase
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

    /// <summary>C05 – "Popular right now": most ordered service items in the last 30 days.</summary>
    [HttpGet("providers/{providerId:guid}")]
    public Task<ProviderSummaryDto> GetProvider(Guid providerId, [FromQuery] Guid? categoryId, CancellationToken ct) =>
        catalog.GetProviderAsync(providerId, categoryId, ct);

    [HttpGet("catalog/products/{productId:guid}")]
    public Task<ProductDto> GetProduct(Guid productId, CancellationToken ct) => catalog.GetProductAsync(productId, ct);

    [HttpGet("catalog/popular")]
    public Task<IReadOnlyList<PopularServiceItemDto>> GetPopular([FromQuery] int take = 6, CancellationToken ct = default) =>
        catalog.GetPopularItemsAsync(take, ct);

    /// <summary>A provider's active catalog (categories → products → service items).</summary>
    [HttpGet("providers/{providerId:guid}/catalog")]
    public Task<ProviderCatalogDto> GetProviderCatalog(Guid providerId, CancellationToken ct) =>
        catalog.GetProviderCatalogAsync(providerId, ct);

    /// <summary>A provider's pickup days and hours (seven days, Sunday first) for the customer's pickup screen.</summary>
    [HttpGet("providers/{providerId:guid}/working-hours")]
    public Task<IReadOnlyList<WorkingHourDto>> GetProviderWorkingHours(Guid providerId, CancellationToken ct) =>
        workingHours.GetAsync(providerId, ct);
}
