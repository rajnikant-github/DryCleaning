using System.Threading.Channels;

namespace DryCleaning.Api.Infrastructure.Realtime;

/// <summary>In-process queue so API requests never wait for open connections.</summary>
public sealed class RealtimeQueue
{
    private readonly Channel<RealtimeBatch> _channel = Channel.CreateBounded<RealtimeBatch>(
        new BoundedChannelOptions(1000) { FullMode = BoundedChannelFullMode.DropOldest, SingleReader = true });

    public void Enqueue(RealtimeBatch batch) => _channel.Writer.TryWrite(batch);

    public IAsyncEnumerable<RealtimeBatch> ReadAllAsync(CancellationToken ct) => _channel.Reader.ReadAllAsync(ct);
}
