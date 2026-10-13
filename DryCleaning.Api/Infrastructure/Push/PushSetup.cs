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
