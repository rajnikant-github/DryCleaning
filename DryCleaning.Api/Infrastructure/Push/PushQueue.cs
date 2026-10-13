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

/// <summary>In-process queue so API requests never wait for Firebase.</summary>
public sealed class PushQueue
{
    private readonly Channel<PushMessage> _channel = Channel.CreateBounded<PushMessage>(
        new BoundedChannelOptions(1000) { FullMode = BoundedChannelFullMode.DropOldest, SingleReader = true });

    public void Enqueue(PushMessage message) => _channel.Writer.TryWrite(message);

    public IAsyncEnumerable<PushMessage> ReadAllAsync(CancellationToken ct) => _channel.Reader.ReadAllAsync(ct);
}
