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
public sealed class ProviderProfileController(ProviderProfileService profiles, ProviderCatalogService catalog, WorkingHoursService workingHours) : ControllerBase
{
    [HttpGet("profile")]
    public Task<ProviderProfileDto> GetProfile(CancellationToken ct) => profiles.GetAsync(ct);

    [HttpPut("profile")]
    public Task<ProviderProfileDto> UpdateProfile(UpdateProviderProfileRequest request, CancellationToken ct) => profiles.UpdateAsync(request, ct);

    /// <summary>All global categories with whether my business offers them.</summary>
    /// <summary>Upload a new business logo (JPG, PNG, HEIC or WEBP). Returns the updated profile with LogoUrl.</summary>
    [HttpPost("profile/logo")]
    [Consumes("multipart/form-data")]
    [RequestSizeLimit(10 * 1024 * 1024)]
    public Task<ProviderProfileDto> UploadLogo([FromForm] UploadFileForm form, CancellationToken ct) => profiles.UploadLogoAsync(form.File, ct);

    [HttpGet("categories")]
    public Task<IReadOnlyList<ProviderCategoryDto>> GetCategories(CancellationToken ct) => catalog.GetCategoriesAsync(ct);

    /// <summary>Choose which global categories my business offers (providers can't create categories).</summary>
    [HttpPut("categories")]
    public Task<IReadOnlyList<ProviderCategoryDto>> SetCategories(SetProviderCategoriesRequest request, CancellationToken ct) =>
        catalog.SetCategoriesAsync(request, ct);

    /// <summary>My pickup days and hours (seven days, Sunday first).</summary>
    [HttpGet("working-hours")]
    public Task<IReadOnlyList<WorkingHourDto>> GetWorkingHours(CancellationToken ct) => workingHours.GetMineAsync(ct);

    /// <summary>Set my pickup days and hours. Customers can only book pickup slots inside them.</summary>
    [HttpPut("working-hours")]
    public Task<IReadOnlyList<WorkingHourDto>> SetWorkingHours(SetWorkingHoursRequest request, CancellationToken ct) =>
        workingHours.SetMineAsync(request, ct);
}
