using DryCleaning.Api.Common;
using DryCleaning.Api.Data;
using DryCleaning.Api.Domain;
using DryCleaning.Api.Dtos;
using DryCleaning.Api.Infrastructure.Email;
using Microsoft.EntityFrameworkCore;

namespace DryCleaning.Api.Services;

/// <summary>
/// Admin announcements: one in-app notification per recipient. Saving them also triggers the push to the user's phones
/// and the live update to open apps, the same as for order notifications.
/// </summary>
public sealed class AdminNotificationService(AppDbContext db, EmailQueue emailQueue)
{
    private const int BatchSize = 500;

    public async Task<int> CountAsync(NotificationAudience audience, Guid? userId, CancellationToken ct) =>
        (await RecipientsAsync(audience, userId, ct)).Count;

    public async Task<SendNotificationResult> SendAsync(SendNotificationRequest request, CancellationToken ct)
    {
        var recipients = await RecipientsAsync(request.Audience, request.UserId, ct);
        if (recipients.Count == 0)
            throw new BadRequestException("There is nobody to send this to.");

        var title = request.Title.Trim();
        var message = request.Message.Trim();
        var emailed = 0;
        foreach (var batch in recipients.Chunk(BatchSize))
        {
            if (request.AlsoEmail)
                foreach (var user in await db.Users.AsNoTracking().Where(u => batch.Contains(u.Id) && u.Email != null).Select(u => new { u.Email, u.Name }).ToListAsync(ct))
                {
                    emailQueue.Enqueue(EmailTemplates.Announcement(user.Email!, user.Name, title, message));
                    emailed++;
                }

            foreach (var userId in batch)
                db.Notify(userId, order: null, title, message, NotificationType.System);

            await db.SaveChangesAsync(ct);
            db.ChangeTracker.Clear(); // keep memory flat for large audiences
        }

        return new SendNotificationResult { Recipients = recipients.Count, EmailsQueued = emailed };
    }

    private async Task<List<Guid>> RecipientsAsync(NotificationAudience audience, Guid? userId, CancellationToken ct)
    {
        if (audience == NotificationAudience.User)
        {
            var user = await db.Users.AsNoTracking().FirstOrDefaultAsync(u => u.Id == userId, ct)
                       ?? throw new NotFoundException("User not found.");
            if (!user.IsActive)
                throw new BadRequestException($"{user.Name}'s account is deactivated.");
            return [user.Id];
        }

        // Group sends go to active customers, providers and drivers – not to admin accounts.
        var query = db.Users.AsNoTracking().Where(u => u.IsActive && u.Role != UserRole.Admin);
        query = audience switch
        {
            NotificationAudience.Customers => query.Where(u => u.Role == UserRole.Customer),
            NotificationAudience.Providers => query.Where(u => u.Role == UserRole.Provider),
            NotificationAudience.Drivers => query.Where(u => u.Role == UserRole.Driver),
            _ => query
        };
        return await query.Select(u => u.Id).ToListAsync(ct);
    }
}
