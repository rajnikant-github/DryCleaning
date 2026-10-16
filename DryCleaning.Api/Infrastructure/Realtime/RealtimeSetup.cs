using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Authentication.JwtBearer;

namespace DryCleaning.Api.Infrastructure.Realtime;

public static class RealtimeSetup
{
    /// <summary>Registers SignalR, the queue, interceptor and background dispatcher.</summary>
    public static IServiceCollection AddRealtime(this IServiceCollection services)
    {
        services.AddSignalR()
            .AddJsonProtocol(options => options.PayloadSerializerOptions.Converters.Add(new JsonStringEnumConverter()));
        services.AddSingleton<RealtimeQueue>();
        services.AddSingleton<RealtimeEventInterceptor>();
        services.AddHostedService<RealtimeDispatcher>();
        return services;
    }

    /// <summary>WebSockets can't send headers, so the hub also accepts the JWT as <c>?access_token=</c>.</summary>
    public static JwtBearerEvents AcceptQueryStringToken() => new()
    {
        OnMessageReceived = context =>
        {
            var token = context.Request.Query["access_token"];
            if (!string.IsNullOrEmpty(token) && context.HttpContext.Request.Path.StartsWithSegments(RealtimeHub.Path))
                context.Token = token;
            return Task.CompletedTask;
        }
    };

    /// <summary>Maps the hub. Connections close when the token expires; the app reconnects with a fresh one.</summary>
    public static void MapRealtime(this IEndpointRouteBuilder app) =>
        app.MapHub<RealtimeHub>(RealtimeHub.Path, options => options.CloseOnAuthenticationExpiration = true);
}
