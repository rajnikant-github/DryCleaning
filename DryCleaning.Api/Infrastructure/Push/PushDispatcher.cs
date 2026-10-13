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

/// <summary>Background worker: reads the queue, looks up the user's devices, sends, removes dead tokens.</summary>
public sealed class PushDispatcher(
    PushQueue queue,
    IServiceScopeFactory scopes,
    IPushSender sender,
    IOptions<PushOptions> options,
    ILogger<PushDispatcher> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        await foreach (var message in queue.ReadAllAsync(stoppingToken))
        {
            if (!options.Value.Enabled)
                continue;

            try
            {
                using var scope = scopes.CreateScope();
                var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
                var tokens = await db.DeviceTokens
                    .Where(d => d.UserId == message.UserId)
                    .Select(d => d.Token)
                    .ToListAsync(stoppingToken);
                if (tokens.Count == 0)
                    continue;

                var dead = await sender.SendAsync(tokens, message, stoppingToken);
                if (dead.Count > 0)
                {
                    await db.DeviceTokens.Where(d => dead.Contains(d.Token)).ExecuteDeleteAsync(stoppingToken);
                    logger.LogInformation("Removed {Count} expired push token(s) for user {UserId}", dead.Count, message.UserId);
                }
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Push for notification {NotificationId} failed", message.NotificationId);
            }
        }
    }
}
