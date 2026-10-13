using DryCleaning.Api.Common;
using DryCleaning.Api.Data;
using DryCleaning.Api.Domain;
using DryCleaning.Api.Dtos;
using DryCleaning.Api.Infrastructure.Storage;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace DryCleaning.Api.Services;

/// <summary>Photo verification: pickup, inspection, completion, delivery package, damage evidence.</summary>
public sealed class OrderPhotoService(AppDbContext db, ICurrentUser currentUser, CurrentProfile profile, IFileStorage storage, IOptions<StorageOptions> options)
{
    private static readonly string[] AllowedExtensions = [".jpg", ".jpeg", ".png", ".heic", ".webp"];

    public async Task<OrderPhotoDto> UploadAsync(Guid orderId, PhotoType type, IFormFile file, CancellationToken ct)
    {
        if (file.Length == 0)
            throw new BadRequestException("The file is empty.");
        if (file.Length > options.Value.MaxUploadBytes)
            throw new BadRequestException($"Photos must be smaller than {options.Value.MaxUploadBytes / (1024 * 1024)} MB.");

        var extension = Path.GetExtension(file.FileName).ToLowerInvariant();
        if (!AllowedExtensions.Contains(extension))
            throw new BadRequestException("Only JPG, PNG, HEIC or WEBP images are allowed.");

        var (order, uploadedBy) = await AuthorizeAsync(orderId, ct);

        await using var stream = file.OpenReadStream();
        var url = await storage.SaveAsync(stream, extension, $"orders/{order.Id:N}", ct);

        var photo = new OrderPhoto { OrderId = order.Id, Type = type, Url = url, UploadedBy = uploadedBy };
        db.OrderPhotos.Add(photo);
        await db.SaveChangesAsync(ct);
        return photo.ToDto();
    }

    public async Task<IReadOnlyList<OrderPhotoDto>> GetAsync(Guid orderId, CancellationToken ct)
    {
        var (order, _) = await AuthorizeAsync(orderId, ct);
        return (await db.OrderPhotos.Where(p => p.OrderId == order.Id).OrderBy(p => p.CreatedAt).ToListAsync(ct))
            .Select(p => p.ToDto()).ToList();
    }

    /// <summary>Customers, the order's provider, its drivers and admins can access photos.</summary>
    private async Task<(Order Order, UploadedBy UploadedBy)> AuthorizeAsync(Guid orderId, CancellationToken ct)
    {
        var order = await db.Orders.Include(o => o.DriverJobs).FirstOrDefaultAsync(o => o.Id == orderId, ct)
                    ?? throw new NotFoundException("Order not found.");

        if (currentUser.Role == UserRole.Admin)
            return (order, UploadedBy.Provider);

        if (currentUser.Role == UserRole.Customer && (await profile.CustomerAsync(ct)).Id == order.CustomerId)
            return (order, UploadedBy.Customer);

        if (currentUser.Role == UserRole.Provider && (await profile.ProviderAsync(ct)).Id == order.ServiceProviderId)
            return (order, UploadedBy.Provider);

        if (currentUser.Role == UserRole.Driver)
        {
            var driver = await profile.DriverAsync(ct);
            if (order.DriverJobs.Any(j => j.DriverId == driver.Id))
                return (order, UploadedBy.Driver);
        }

        throw new NotFoundException("Order not found.");
    }
}

public sealed class NotificationService(AppDbContext db, CurrentProfile profile)
{
    public async Task<PagedResult<NotificationDto>> GetAsync(PagedRequest request, bool unreadOnly, CancellationToken ct)
    {
        var userId = profile.UserId;
        var query = db.Notifications.Where(n => n.UserId == userId && (!unreadOnly || !n.IsRead));
        var (items, total) = await query.OrderByDescending(n => n.CreatedAt).PageAsync(request, ct);
        return new PagedResult<NotificationDto>(items.Select(n => n.ToDto()).ToList(), request.Page, request.PageSize, total);
    }

    public Task<int> UnreadCountAsync(CancellationToken ct)
    {
        var userId = profile.UserId;
        return db.Notifications.CountAsync(n => n.UserId == userId && !n.IsRead, ct);
    }

    public async Task MarkReadAsync(Guid id, CancellationToken ct)
    {
        var userId = profile.UserId;
        var notification = await db.Notifications.FirstOrDefaultAsync(n => n.Id == id && n.UserId == userId, ct)
                           ?? throw new NotFoundException("Notification not found.");
        notification.IsRead = true;
        await db.SaveChangesAsync(ct);
    }

    public Task<int> MarkAllReadAsync(CancellationToken ct)
    {
        var userId = profile.UserId;
        return db.Notifications.Where(n => n.UserId == userId && !n.IsRead)
            .ExecuteUpdateAsync(s => s.SetProperty(n => n.IsRead, true).SetProperty(n => n.UpdatedAt, DateTime.UtcNow), ct);
    }
}

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

        return new ProviderProfileDto(provider.Id, provider.BusinessName, provider.Description, provider.Phone, provider.Email,
            provider.LogoUrl, provider.IsActive, owner.ToDto(provider.Id), provider.Address?.ToDto(),
            categories.Select(c => c.ToDto()).ToList());
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
