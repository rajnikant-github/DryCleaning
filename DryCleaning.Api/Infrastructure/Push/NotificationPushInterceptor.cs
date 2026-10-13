using System.Runtime.CompilerServices;
using System.Threading.Channels;
using DryCleaning.Api.Common;
using DryCleaning.Api.Data;
using FirebaseAdmin;
using FirebaseAdmin.Messaging;
using Google.Apis.Auth.OAuth2;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.Options;

namespace DryCleaning.Api.Infrastructure.Push;

/// <summary>
/// Every in-app <see cref="Domain.Notification"/> saved to the database is also pushed to the user's phones.
/// Notifications are captured before SaveChanges and queued only after it succeeds.
/// </summary>
public sealed class NotificationPushInterceptor(PushQueue queue) : SaveChangesInterceptor
{
    private readonly ConditionalWeakTable<DbContext, List<PushMessage>> _pending = new();

    public override InterceptionResult<int> SavingChanges(DbContextEventData eventData, InterceptionResult<int> result)
    {
        Capture(eventData.Context);
        return base.SavingChanges(eventData, result);
    }

    public override ValueTask<InterceptionResult<int>> SavingChangesAsync(
        DbContextEventData eventData, InterceptionResult<int> result, CancellationToken cancellationToken = default)
    {
        Capture(eventData.Context);
        return base.SavingChangesAsync(eventData, result, cancellationToken);
    }

    public override int SavedChanges(SaveChangesCompletedEventData eventData, int result)
    {
        Flush(eventData.Context);
        return base.SavedChanges(eventData, result);
    }

    public override ValueTask<int> SavedChangesAsync(
        SaveChangesCompletedEventData eventData, int result, CancellationToken cancellationToken = default)
    {
        Flush(eventData.Context);
        return base.SavedChangesAsync(eventData, result, cancellationToken);
    }

    public override void SaveChangesFailed(DbContextErrorEventData eventData)
    {
        Discard(eventData.Context);
        base.SaveChangesFailed(eventData);
    }

    public override Task SaveChangesFailedAsync(DbContextErrorEventData eventData, CancellationToken cancellationToken = default)
    {
        Discard(eventData.Context);
        return base.SaveChangesFailedAsync(eventData, cancellationToken);
    }

    private void Capture(DbContext? context)
    {
        if (context is null)
            return;

        var added = context.ChangeTracker.Entries<Domain.Notification>()
            .Where(e => e.State == EntityState.Added)
            .Select(e => new PushMessage
            {
                NotificationId = e.Entity.Id,
                UserId = e.Entity.UserId,
                Title = e.Entity.Title,
                Body = e.Entity.Message,
                OrderId = e.Entity.OrderId,
                Type = e.Entity.Type.ToString()
            })
            .ToList();

        if (added.Count > 0)
            _pending.AddOrUpdate(context, added);
        else
            _pending.Remove(context);
    }

    private void Flush(DbContext? context)
    {
        if (context is null || !_pending.TryGetValue(context, out var messages))
            return;
        _pending.Remove(context);
        foreach (var message in messages)
            queue.Enqueue(message);
    }

    private void Discard(DbContext? context)
    {
        if (context is not null)
            _pending.Remove(context);
    }
}
