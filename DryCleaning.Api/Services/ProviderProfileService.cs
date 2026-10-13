using DryCleaning.Api.Common;
using DryCleaning.Api.Data;
using DryCleaning.Api.Domain;
using DryCleaning.Api.Dtos;
using DryCleaning.Api.Infrastructure.Storage;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace DryCleaning.Api.Services;

public sealed class ProviderProfileService(AppDbContext db, CurrentProfile profile)
{
    public async Task<ProviderProfileDto> GetAsync(CancellationToken ct)
    {
        var provider = await profile.ProviderAsync(ct);
        var owner = await db.Users.FirstAsync(u => u.Id == provider.UserId, ct);
        var categories = await db.ServiceProviderCategories
            .Where(pc => pc.ServiceProviderId == provider.Id && pc.IsActive)
            .Select(pc => pc.Category)
            .OrderBy(c => c.DisplayOrder)
            .ToListAsync(ct);

        return new ProviderProfileDto
        {
            Id = provider.Id,
            BusinessName = provider.BusinessName,
            Description = provider.Description,
            Phone = provider.Phone,
            Email = provider.Email,
            LogoUrl = provider.LogoUrl,
            IsActive = provider.IsActive,
            Owner = owner.ToDto(provider.Id),
            Address = provider.Address?.ToDto(),
            Categories = categories.Select(c => c.ToDto()).ToList()
        };
    }

    public async Task<ProviderProfileDto> UpdateAsync(UpdateProviderProfileRequest request, CancellationToken ct)
    {
        var provider = await profile.ProviderAsync(ct);
        provider.BusinessName = request.BusinessName.Trim();
        provider.Description = request.Description;
        provider.Phone = request.Phone;
        provider.Email = request.Email;
        provider.LogoUrl = request.LogoUrl;
        await db.SaveChangesAsync(ct);
        return await GetAsync(ct);
    }
}
