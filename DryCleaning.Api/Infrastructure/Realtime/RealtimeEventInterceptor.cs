using System.Runtime.CompilerServices;
using DryCleaning.Api.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Diagnostics;

namespace DryCleaning.Api.Infrastructure.Realtime;

/// <summary>
/// Watches orders, payments, driver jobs and notifications. Changes are captured before SaveChanges and
/// queued for <see cref="RealtimeDispatcher"/> only after it succeeds, so business services need no extra code.
/// </summary>
public sealed class RealtimeEventInterceptor(RealtimeQueue queue) : SaveChangesInterceptor
{
    private readonly ConditionalWeakTable<DbContext, RealtimeBatch> _pending = new();

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

        var batch = new RealtimeBatch();
        var tracker = context.ChangeTracker;

        foreach (var entry in tracker.Entries<Order>())
            if (entry.State == EntityState.Added || IsModified(entry, nameof(Order.Status)))
                batch.OrderIds.Add(entry.Entity.Id);

        // Paid orders appear in the provider's New tab without a status change.
        foreach (var entry in tracker.Entries<Payment>())
            if (IsModified(entry, nameof(Payment.Status)))
                batch.OrderIds.Add(entry.Entity.OrderId);

        foreach (var entry in tracker.Entries<DriverJob>())
        {
            var added = entry.State == EntityState.Added;
            if (!added && !IsModified(entry, nameof(DriverJob.Status)) && !IsModified(entry, nameof(DriverJob.DriverId)))
                continue;

            var job = entry.Entity;
            var previousDriverId = added ? null : (Guid?)entry.Property(nameof(DriverJob.DriverId)).OriginalValue;
            batch.Jobs.Add(new CapturedJobChange
            {
                JobId = job.Id,
                OrderId = job.OrderId,
                JobType = job.JobType,
                Status = job.Status,
                DriverId = job.DriverId,
                PreviousDriverId = previousDriverId != job.DriverId ? previousDriverId : null
            });
            batch.OrderIds.Add(job.OrderId);
        }

        foreach (var entry in tracker.Entries<Notification>().Where(e => e.State == EntityState.Added))
        {
            var n = entry.Entity;
            batch.Notifications.Add(new NotificationReceivedEvent
            {
                NotificationId = n.Id,
                UserId = n.UserId,
                Title = n.Title,
                Message = n.Message,
                Type = n.Type,
                OrderId = n.OrderId,
                CreatedAt = n.CreatedAt
            });
        }

        if (batch.IsEmpty)
            _pending.Remove(context);
        else
            _pending.AddOrUpdate(context, batch);
    }

    private static bool IsModified(EntityEntry entry, string property) =>
        entry.State == EntityState.Modified && entry.Property(property).IsModified;

    private void Flush(DbContext? context)
    {
        if (context is null || !_pending.TryGetValue(context, out var batch))
            return;
        _pending.Remove(context);
        queue.Enqueue(batch);
    }

    private void Discard(DbContext? context)
    {
        if (context is not null)
            _pending.Remove(context);
    }
}
