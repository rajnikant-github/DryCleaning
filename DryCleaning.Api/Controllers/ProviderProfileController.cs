using DryCleaning.Api.Common;
using DryCleaning.Api.Dtos;
using DryCleaning.Api.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace DryCleaning.Api.Controllers;

/// <summary>Service provider business profile and enabled categories.</summary>
[ApiController]
[Route("api/provider")]
[Authorize(Roles = Roles.Provider)]
[Produces("application/json")]
public sealed class ProviderProfileController(ProviderProfileService profiles, ProviderCatalogService catalog) : ControllerBase
{
    [HttpGet("profile")]
    public Task<ProviderProfileDto> GetProfile(CancellationToken ct) => profiles.GetAsync(ct);

    [HttpPut("profile")]
    public Task<ProviderProfileDto> UpdateProfile(UpdateProviderProfileRequest request, CancellationToken ct) => profiles.UpdateAsync(request, ct);

    /// <summary>All global categories with whether my business offers them.</summary>
    [HttpGet("categories")]
    public Task<IReadOnlyList<ProviderCategoryDto>> GetCategories(CancellationToken ct) => catalog.GetCategoriesAsync(ct);

    /// <summary>Choose which global categories my business offers (providers can't create categories).</summary>
    [HttpPut("categories")]
    public Task<IReadOnlyList<ProviderCategoryDto>> SetCategories(SetProviderCategoriesRequest request, CancellationToken ct) =>
        catalog.SetCategoriesAsync(request, ct);
}
