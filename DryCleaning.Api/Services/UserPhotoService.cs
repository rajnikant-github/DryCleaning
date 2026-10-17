using DryCleaning.Api.Common;
using DryCleaning.Api.Data;
using DryCleaning.Api.Domain;
using DryCleaning.Api.Dtos;
using DryCleaning.Api.Infrastructure.Storage;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace DryCleaning.Api.Services;

/// <summary>The signed-in user's profile photo (customers, drivers and providers' owners).</summary>
public sealed class UserPhotoService(AppDbContext db, CurrentProfile profile, IFileStorage storage, IOptions<StorageOptions> options)
{
    private static readonly string[] AllowedExtensions = [".jpg", ".jpeg", ".png", ".heic", ".webp"];

    public async Task<UserDto> UploadAsync(IFormFile file, CancellationToken ct)
    {
        if (file.Length == 0)
            throw new BadRequestException("The file is empty.");
        if (file.Length > options.Value.MaxUploadBytes)
            throw new BadRequestException($"Photos must be smaller than {options.Value.MaxUploadBytes / (1024 * 1024)} MB.");

        var extension = Path.GetExtension(file.FileName).ToLowerInvariant();
        if (!AllowedExtensions.Contains(extension))
            throw new BadRequestException("Only JPG, PNG, HEIC or WEBP images are allowed.");

        var user = await LoadUserAsync(ct);
        await using var stream = file.OpenReadStream();
        // Public-read: customers see their driver's photo on the tracking screen.
        user.PhotoUrl = await storage.SaveAsync(stream, extension, $"users/{user.Id:N}", FileVisibility.Public, ct);
        await db.SaveChangesAsync(ct);
        return await ToDtoAsync(user, ct);
    }

    public async Task<UserDto> RemoveAsync(CancellationToken ct)
    {
        var user = await LoadUserAsync(ct);
        user.PhotoUrl = null;
        await db.SaveChangesAsync(ct);
        return await ToDtoAsync(user, ct);
    }

    private Task<User> LoadUserAsync(CancellationToken ct) =>
        db.Users.FirstAsync(u => u.Id == profile.UserId, ct);

    private async Task<UserDto> ToDtoAsync(User user, CancellationToken ct)
    {
        var profileId = user.Role switch
        {
            UserRole.Customer => (Guid?)(await profile.CustomerAsync(ct)).Id,
            UserRole.Provider => (await profile.ProviderAsync(ct)).Id,
            UserRole.Driver => (await profile.DriverAsync(ct)).Id,
            _ => null
        };
        return user.ToDto(profileId);
    }
}
