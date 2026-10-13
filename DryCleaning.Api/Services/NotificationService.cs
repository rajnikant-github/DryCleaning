using DryCleaning.Api.Common;
using DryCleaning.Api.Data;
using DryCleaning.Api.Domain;
using DryCleaning.Api.Dtos;
using DryCleaning.Api.Infrastructure.Storage;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace DryCleaning.Api.Services;

public sealed class NotificationService(AppDbContext db, CurrentProfile profile)
{
    public async Task<PagedResult<NotificationDto>> GetAsync(PagedRequest request, bool unreadOnly, CancellationToken ct)
    {
        var userId = profile.UserId;
        var query = db.Notifications.Where(n => n.UserId == userId && (!unreadOnly || !n.IsRead));
        var (items, total) = await query.OrderByDescending(n => n.CreatedAt).PageAsync(request, ct);
        return new PagedResult<NotificationDto>
        {
            Items = items.Select(n => n.ToDto()).ToList(),
            Page = request.Page,
            PageSize = request.PageSize,
            TotalCount = total
        };
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
