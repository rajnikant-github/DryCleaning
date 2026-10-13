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

/// <summary>One push for one user (sent to all of that user's registered phones).</summary>
public sealed record PushMessage(Guid NotificationId, Guid UserId, string Title, string Body, Guid? OrderId, string Type);

/// <summary>In-process queue so API requests never wait for Firebase.</summary>
public sealed class PushQueue
{
    private readonly Channel<PushMessage> _channel = Channel.CreateBounded<PushMessage>(
        new BoundedChannelOptions(1000) { FullMode = BoundedChannelFullMode.DropOldest, SingleReader = true });

    public void Enqueue(PushMessage message) => _channel.Writer.TryWrite(message);

    public IAsyncEnumerable<PushMessage> ReadAllAsync(CancellationToken ct) => _channel.Reader.ReadAllAsync(ct);
}

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
            .Select(e => new PushMessage(e.Entity.Id, e.Entity.UserId, e.Entity.Title, e.Entity.Message, e.Entity.OrderId, e.Entity.Type.ToString()))
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

public interface IPushSender
{
    /// <summary>Sends to the given FCM tokens. Returns tokens Firebase reported as no longer valid.</summary>
    Task<IReadOnlyList<string>> SendAsync(IReadOnlyList<string> tokens, PushMessage message, CancellationToken ct);
}

/// <summary>Used when Firebase isn't configured: pushes are only written to the log.</summary>
public sealed class LoggingPushSender(ILogger<LoggingPushSender> logger) : IPushSender
{
    public Task<IReadOnlyList<string>> SendAsync(IReadOnlyList<string> tokens, PushMessage message, CancellationToken ct)
    {
        logger.LogInformation("Push (not sent – Firebase not configured) to {Count} device(s) of user {UserId}: {Title} – {Body}",
            tokens.Count, message.UserId, message.Title, message.Body);
        return Task.FromResult<IReadOnlyList<string>>([]);
    }
}

/// <summary>Firebase Cloud Messaging – one API for Android and iOS (APNs is configured in the Firebase console).</summary>
public sealed class FcmPushSender : IPushSender
{
    private static readonly MessagingErrorCode[] DeadTokenErrors =
        [MessagingErrorCode.Unregistered, MessagingErrorCode.InvalidArgument, MessagingErrorCode.SenderIdMismatch];

    private readonly FirebaseMessaging _messaging;
    private readonly ILogger<FcmPushSender> _logger;

    public FcmPushSender(string credentialsPath, ILogger<FcmPushSender> logger)
    {
        _logger = logger;
        var app = FirebaseApp.GetInstance("edry") ?? FirebaseApp.Create(
            new AppOptions { Credential = GoogleCredential.FromFile(credentialsPath) }, "edry");
        _messaging = FirebaseMessaging.GetMessaging(app);
    }

    public async Task<IReadOnlyList<string>> SendAsync(IReadOnlyList<string> tokens, PushMessage message, CancellationToken ct)
    {
        var data = new Dictionary<string, string>
        {
            ["notificationId"] = message.NotificationId.ToString(),
            ["type"] = message.Type
        };
        if (message.OrderId is { } orderId)
            data["orderId"] = orderId.ToString();

        var dead = new List<string>();
        foreach (var batch in tokens.Chunk(500)) // FCM multicast limit
        {
            var response = await _messaging.SendEachForMulticastAsync(new MulticastMessage
            {
                Tokens = batch,
                Notification = new FirebaseAdmin.Messaging.Notification { Title = message.Title, Body = message.Body },
                Data = data,
                Android = new AndroidConfig { Priority = Priority.High },
                Apns = new ApnsConfig { Aps = new Aps { Sound = "default" } }
            }, ct);

            for (var i = 0; i < response.Responses.Count; i++)
            {
                var result = response.Responses[i];
                if (result.IsSuccess)
                    continue;
                if (result.Exception?.MessagingErrorCode is { } code && DeadTokenErrors.Contains(code))
                    dead.Add(batch[i]);
                else
                    _logger.LogWarning(result.Exception, "Push to a device of user {UserId} failed", message.UserId);
            }
        }

        return dead;
    }
}

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

public static class PushSetup
{
    /// <summary>Registers the queue, interceptor, sender (Firebase or log-only) and background dispatcher.</summary>
    public static IServiceCollection AddPushNotifications(this IServiceCollection services, IConfiguration config)
    {
        services.Configure<PushOptions>(config.GetSection(PushOptions.Section));
        services.AddSingleton<PushQueue>();
        services.AddSingleton<NotificationPushInterceptor>();
        services.AddHostedService<PushDispatcher>();

        var push = config.GetSection(PushOptions.Section).Get<PushOptions>() ?? new PushOptions();
        var path = push.FirebaseCredentialsPath;
        if (push.Enabled && !string.IsNullOrWhiteSpace(path) && File.Exists(path))
            services.AddSingleton<IPushSender>(sp => new FcmPushSender(path, sp.GetRequiredService<ILogger<FcmPushSender>>()));
        else
            services.AddSingleton<IPushSender, LoggingPushSender>();

        return services;
    }
}
