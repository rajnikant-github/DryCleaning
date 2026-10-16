using DryCleaning.Api.Data;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;

namespace DryCleaning.Api.Infrastructure.Realtime;

/// <summary>Background worker: reads the queue, works out who should hear about each change, sends through the hub.</summary>
public sealed class RealtimeDispatcher(
    RealtimeQueue queue,
    IServiceScopeFactory scopes,
    IHubContext<RealtimeHub> hub,
    ILogger<RealtimeDispatcher> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        await foreach (var batch in queue.ReadAllAsync(stoppingToken))
        {
            try
            {
                using var scope = scopes.CreateScope();
                var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

                await SendOrdersAsync(db, batch, stoppingToken);
                await SendJobsAsync(db, batch, stoppingToken);
                foreach (var notification in batch.Notifications)
                    await hub.Clients.User(notification.UserId.ToString())
                        .SendAsync(RealtimeEvents.NotificationReceived, notification, stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Real-time dispatch failed");
            }
        }
    }

    private async Task SendOrdersAsync(AppDbContext db, RealtimeBatch batch, CancellationToken ct)
    {
        if (batch.OrderIds.Count == 0)
            return;

        var orders = await db.Orders.AsNoTracking()
            .Where(o => batch.OrderIds.Contains(o.Id))
            .Select(o => new
            {
                o.Id,
                o.OrderNumber,
                o.Status,
                CustomerUserId = o.Customer.UserId,
                ProviderUserId = o.ServiceProvider.UserId,
                DriverUserIds = o.DriverJobs.Where(j => j.Driver != null).Select(j => j.Driver!.UserId).ToList()
            })
            .ToListAsync(ct);

        foreach (var order in orders)
        {
            var users = order.DriverUserIds
                .Append(order.CustomerUserId)
                .Append(order.ProviderUserId)
                .Distinct()
                .Select(id => id.ToString())
                .ToList();
            var payload = new OrderUpdatedEvent { OrderId = order.Id, OrderNumber = order.OrderNumber, Status = order.Status };
            await hub.Clients.Users(users).SendAsync(RealtimeEvents.OrderUpdated, payload, ct);
        }
    }

    private async Task SendJobsAsync(AppDbContext db, RealtimeBatch batch, CancellationToken ct)
    {
        if (batch.Jobs.Count == 0)
            return;

        // Online drivers see jobs appear and disappear; the job's current and previous driver always hear about it.
        var onlineDriverUserIds = await db.Drivers.AsNoTracking()
            .Where(d => d.IsAvailable && d.IsActive && d.User.IsActive)
            .Select(d => d.UserId)
            .ToListAsync(ct);

        var jobDriverIds = batch.Jobs
            .SelectMany(j => new[] { j.DriverId, j.PreviousDriverId })
            .OfType<Guid>()
            .Distinct()
            .ToList();
        var driverUserIds = jobDriverIds.Count == 0
            ? new Dictionary<Guid, Guid>()
            : await db.Drivers.AsNoTracking()
                .Where(d => jobDriverIds.Contains(d.Id))
                .ToDictionaryAsync(d => d.Id, d => d.UserId, ct);

        foreach (var job in batch.Jobs)
        {
            var users = new HashSet<Guid>(onlineDriverUserIds);
            foreach (var driverId in new[] { job.DriverId, job.PreviousDriverId })
                if (driverId is { } id && driverUserIds.TryGetValue(id, out var userId))
                    users.Add(userId);

            var payload = new DriverJobChangedEvent { JobId = job.JobId, OrderId = job.OrderId, JobType = job.JobType, Status = job.Status };
            await hub.Clients.Users(users.Select(u => u.ToString()).ToList())
                .SendAsync(RealtimeEvents.DriverJobChanged, payload, ct);
        }
    }
}
