using DryCleaning.Api.Domain;

namespace DryCleaning.Api.Infrastructure.Push;

/// <summary>Used when a platform isn't configured: pushes are only written to the log.</summary>
public sealed class LoggingPushSender(DevicePlatform platform, ILogger<LoggingPushSender> logger) : IPushSender
{
    public DevicePlatform Platform { get; } = platform;

    public Task<IReadOnlyList<string>> SendAsync(IReadOnlyList<string> tokens, PushMessage message, CancellationToken ct)
    {
        logger.LogInformation("Push (not sent – {Platform} push not configured) to {Count} device(s) of user {UserId}: {Title} – {Body}",
            Platform, tokens.Count, message.UserId, message.Title, message.Body);
        return Task.FromResult<IReadOnlyList<string>>([]);
    }
}
