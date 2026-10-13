using DryCleaning.Models;

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
                LogoUrl = dto.LogoUrl,
                IsActive = dto.IsActive
            },
            Owner = ApiMapping.ToUser(dto.Owner),
            Address = address,
            Categories = dto.Categories.OrderBy(c => c.DisplayOrder).Select(ApiMapping.ToCategory).ToList()
        };
    }
}
