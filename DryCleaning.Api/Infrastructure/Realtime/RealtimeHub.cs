using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;

namespace DryCleaning.Api.Infrastructure.Realtime;

/// <summary>
/// Live updates for signed-in apps (server → client only). Events are small "something changed" signals;
/// the app re-reads the details through the REST endpoints. Method names are in <see cref="RealtimeEvents"/>.
/// </summary>
[Authorize]
public sealed class RealtimeHub : Hub
{
    public const string Path = "/hubs/realtime";
}
