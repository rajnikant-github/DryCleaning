using DryCleaning.Api.Common;
using DryCleaning.Api.Data;
using DryCleaning.Api.Domain;
using DryCleaning.Api.Dtos;
using DryCleaning.Api.Infrastructure.Storage;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace DryCleaning.Api.Services;

public sealed class ProviderProfileService(AppDbContext db, CurrentProfile profile, IFileStorage storage, IOptions<StorageOptions> options)
{
    private static readonly string[] AllowedLogoExtensions = [".jpg", ".jpeg", ".png", ".heic", ".webp"];

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
            LogoUrl = FileUrls.Resolve(provider.LogoUrl),
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
        // The app echoes back the URL it was given; keep the stored path unless the logo really changed.
        if (request.LogoUrl != FileUrls.Resolve(provider.LogoUrl))
            provider.LogoUrl = request.LogoUrl;
        await db.SaveChangesAsync(ct);
        return await GetAsync(ct);
    }

    /// <summary>Saves a new business logo (multipart upload) and returns the updated profile.</summary>
    public async Task<ProviderProfileDto> UploadLogoAsync(IFormFile file, CancellationToken ct)
    {
        if (file.Length == 0)
            throw new BadRequestException("The file is empty.");
        if (file.Length > options.Value.MaxUploadBytes)
            throw new BadRequestException($"Logos must be smaller than {options.Value.MaxUploadBytes / (1024 * 1024)} MB.");

        var extension = Path.GetExtension(file.FileName).ToLowerInvariant();
        if (!AllowedLogoExtensions.Contains(extension))
            throw new BadRequestException("Only JPG, PNG, HEIC or WEBP images are allowed.");

        var provider = await profile.ProviderAsync(ct);
        await using var stream = file.OpenReadStream();
        provider.LogoUrl = await storage.SaveAsync(stream, extension, $"providers/{provider.Id:N}", FileVisibility.Public, ct);
        await db.SaveChangesAsync(ct);
        return await GetAsync(ct);
    }
}
