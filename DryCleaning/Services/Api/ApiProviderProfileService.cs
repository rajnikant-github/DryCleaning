using DryCleaning.Models;
using DryCleaning.Services.Api.Contracts;

namespace DryCleaning.Services.Api;

/// <summary>The signed-in provider's business profile.</summary>
public sealed class ApiProviderProfileService(ApiClient api) : IProviderProfileService
{
    public async Task<ProviderProfile> GetProfileAsync()
    {
        var dto = await api.GetAsync("api/provider/profile", ApiJsonContext.Default.ProviderProfileDto);
        var address = dto.Address is null ? new Address() : ApiMapping.ToAddress(dto.Address);
        return new ProviderProfile
        {
            Provider = new Provider
            {
                Id = dto.Id,
                UserId = dto.Owner.Id,
                AddressId = address.Id,
                BusinessName = dto.BusinessName,
                Description = dto.Description ?? string.Empty,
                Phone = dto.Phone ?? string.Empty,
                Email = dto.Email ?? string.Empty,
                LogoUrl = dto.LogoUrl is null ? null : api.AbsoluteUrl(dto.LogoUrl),
                IsActive = dto.IsActive
            },
            Owner = ApiMapping.ToUser(dto.Owner),
            Address = address,
            Categories = dto.Categories.OrderBy(c => c.DisplayOrder).Select(ApiMapping.ToCategory).ToList()
        };
    }

    public Task UpdateProfileAsync(ProviderProfileUpdate update) =>
        api.SendAsync(HttpMethod.Put, "api/provider/profile",
            new UpdateProviderProfileRequest
            {
                BusinessName = update.BusinessName.Trim(),
                Description = Clean(update.Description),
                Phone = Clean(update.Phone),
                Email = Clean(update.Email),
                LogoUrl = api.RelativeUrl(update.LogoUrl)
            },
            ApiJsonContext.Default.UpdateProviderProfileRequest);

    public async Task<string> UploadLogoAsync(string localPath)
    {
        var dto = await api.UploadFileAsync("api/provider/profile/logo", localPath, ApiJsonContext.Default.ProviderProfileDto);
        return dto.LogoUrl is null ? string.Empty : api.AbsoluteUrl(dto.LogoUrl);
    }

    public async Task<IReadOnlyList<ProviderCategoryOption>> GetCategoryOptionsAsync()
    {
        var mineTask = api.GetAsync("api/provider/categories", ApiJsonContext.Default.ListProviderCategoryDto);
        var allTask = api.GetAsync("api/categories", ApiJsonContext.Default.ListCategoryDto, authorize: false);
        await Task.WhenAll(mineTask, allTask);

        var details = allTask.Result.ToDictionary(c => c.Id);
        return mineTask.Result
            .Select(c => new ProviderCategoryOption
            {
                CategoryId = c.CategoryId,
                Name = c.Name,
                Description = details.TryGetValue(c.CategoryId, out var d) ? d.Description ?? string.Empty : string.Empty,
                DisplayOrder = details.TryGetValue(c.CategoryId, out var o) ? o.DisplayOrder : int.MaxValue,
                IsEnabled = c.IsEnabled
            })
            .OrderBy(c => c.DisplayOrder)
            .ToList();
    }

    public async Task SetCategoriesAsync(IReadOnlyList<Guid> categoryIds)
    {
        if (categoryIds.Count == 0)
            throw new InvalidOperationException("Choose at least one category.");

        await api.SendAsync(HttpMethod.Put, "api/provider/categories",
            new SetProviderCategoriesRequest { CategoryIds = categoryIds.Distinct().ToList() },
            ApiJsonContext.Default.SetProviderCategoriesRequest, ApiJsonContext.Default.ListProviderCategoryDto);
    }

    private static string? Clean(string? value) => string.IsNullOrWhiteSpace(value) ? null : value.Trim();
}
