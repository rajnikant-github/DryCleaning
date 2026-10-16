using System.Diagnostics;
using System.Text.Json;
using CommunityToolkit.Mvvm.Messaging;
using DryCleaning.Services.Realtime;
using Microsoft.AspNetCore.SignalR.Client;

namespace DryCleaning.Services.Api;

/// <summary>
/// SignalR connection to the API's /hubs/realtime. The hub only says "something changed"; screens re-read the
/// details through the normal REST services. The access token is fetched (and refreshed) on every (re)connect.
/// </summary>
public sealed class ApiRealtimeService(ApiClient api, ISessionService session) : IRealtimeService
{
    private const string HubPath = "hubs/realtime";

    private readonly object _lock = new();
    private HubConnection? _connection;
    private CancellationTokenSource? _started;
    private bool _hasConnectedBefore;

    public bool IsConnected => _connection?.State == HubConnectionState.Connected;

    public Task StartAsync()
    {
        if (!session.IsSignedIn)
            return Task.CompletedTask;

        CancellationToken ct;
        lock (_lock)
        {
            if (_started is not null)
                return Task.CompletedTask;
            _started = new CancellationTokenSource();
            ct = _started.Token;
            _connection ??= Build();
        }

        _ = Task.Run(() => ConnectAsync(ct), ct);
        return Task.CompletedTask;
    }

    public async Task StopAsync()
    {
        HubConnection? connection;
        lock (_lock)
        {
            _started?.Cancel();
            _started = null;
            connection = _connection;
        }

        if (connection is null)
            return;

        try
        {
            await connection.StopAsync();
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"[Realtime] Stop failed: {ex.Message}");
        }
    }

    private HubConnection Build()
    {
        var connection = new HubConnectionBuilder()
            .WithUrl(new Uri(api.BaseAddress, HubPath), options => options.AccessTokenProvider = GetTokenAsync)
            .WithAutomaticReconnect(new RealtimeRetryPolicy())
            .Build();

        connection.On<JsonElement>("OrderUpdated", p => Publish(new RealtimeChangedMessage
        {
            Kind = RealtimeChangeKind.OrderUpdated,
            OrderId = Guid(p, "orderId"),
            Status = Text(p, "status")
        }));
        connection.On<JsonElement>("DriverJobChanged", p => Publish(new RealtimeChangedMessage
        {
            Kind = RealtimeChangeKind.JobChanged,
            JobId = Guid(p, "jobId"),
            OrderId = Guid(p, "orderId"),
            Status = Text(p, "status")
        }));
        connection.On<JsonElement>("NotificationReceived", p => Publish(new RealtimeChangedMessage
        {
            Kind = RealtimeChangeKind.NotificationReceived,
            OrderId = Guid(p, "orderId")
        }));

        connection.Reconnected += _ =>
        {
            Publish(new RealtimeChangedMessage { Kind = RealtimeChangeKind.Reconnected });
            return Task.CompletedTask;
        };

        // The server closes the connection when the token expires; reconnect with a fresh one unless we stopped on purpose.
        connection.Closed += error =>
        {
            CancellationToken ct;
            lock (_lock)
            {
                if (_started is null)
                    return Task.CompletedTask;
                ct = _started.Token;
            }

            _ = Task.Run(() => ConnectAsync(ct), ct);
            return Task.CompletedTask;
        };

        return connection;
    }

    private async Task ConnectAsync(CancellationToken ct)
    {
        for (var attempt = 0; !ct.IsCancellationRequested; attempt++)
        {
            try
            {
                if (_connection is not { } connection || connection.State is HubConnectionState.Connected or HubConnectionState.Connecting or HubConnectionState.Reconnecting)
                    return;
                await connection.StartAsync(ct);
                if (_hasConnectedBefore)
                    Publish(new RealtimeChangedMessage { Kind = RealtimeChangeKind.Reconnected });
                _hasConnectedBefore = true;
                return;
            }
            catch (OperationCanceledException) when (ct.IsCancellationRequested)
            {
                return;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[Realtime] Connect failed (attempt {attempt + 1}): {ex.Message}");
                try
                {
                    await Task.Delay(RealtimeRetryPolicy.DelayFor(attempt), ct);
                }
                catch (OperationCanceledException)
                {
                    return;
                }
            }
        }
    }

    private async Task<string?> GetTokenAsync()
    {
        try
        {
            return await api.GetAccessTokenAsync();
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"[Realtime] No access token: {ex.Message}");
            return null;
        }
    }

    private static void Publish(RealtimeChangedMessage message) => WeakReferenceMessenger.Default.Send(message);

    private static Guid? Guid(JsonElement element, string name) =>
        element.TryGetProperty(name, out var value) && value.ValueKind == JsonValueKind.String && System.Guid.TryParse(value.GetString(), out var id)
            ? id
            : null;

    private static string? Text(JsonElement element, string name) =>
        element.TryGetProperty(name, out var value) && value.ValueKind == JsonValueKind.String ? value.GetString() : null;
}
