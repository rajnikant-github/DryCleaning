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
