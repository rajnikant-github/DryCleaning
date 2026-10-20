using DryCleaning.Api.Common;
using DryCleaning.Api.Domain;

namespace DryCleaning.Api.Infrastructure.Push;

public static class PushSetup
{
    /// <summary>
    /// Registers the queue, interceptor, one sender per platform (Android: Firebase, iOS: Apple APNs;
    /// either falls back to log-only when it isn't configured) and the background dispatcher.
    /// </summary>
    public static IServiceCollection AddPushNotifications(this IServiceCollection services, IConfiguration config, IHostEnvironment environment)
    {
        services.Configure<PushOptions>(config.GetSection(PushOptions.Section));
        services.Configure<ApnsOptions>(config.GetSection(ApnsOptions.Section));
        services.AddSingleton<PushQueue>();
        services.AddSingleton<NotificationPushInterceptor>();
        services.AddHostedService<PushDispatcher>();

        var push = config.GetSection(PushOptions.Section).Get<PushOptions>() ?? new PushOptions();

        // Android – Firebase Cloud Messaging
        var firebasePath = push.FirebaseCredentialsPath;
        if (push.Enabled && !string.IsNullOrWhiteSpace(firebasePath) && File.Exists(firebasePath))
            services.AddSingleton<IPushSender>(sp => new FcmPushSender(firebasePath, sp.GetRequiredService<ILogger<FcmPushSender>>()));
        else
            services.AddSingleton<IPushSender>(sp => new LoggingPushSender(DevicePlatform.Android, sp.GetRequiredService<ILogger<LoggingPushSender>>()));

        // iOS – Apple Push Notification service
        var apns = config.GetSection(ApnsOptions.Section).Get<ApnsOptions>() ?? new ApnsOptions();
        var keyPath = apns.IsConfigured ? Path.GetFullPath(apns.PrivateKeyPath!, environment.ContentRootPath) : null;
        if (push.Enabled && keyPath is not null && File.Exists(keyPath))
            services.AddSingleton<IPushSender>(sp =>
                new ApnsPushSender(apns, File.ReadAllText(keyPath), handler: null, sp.GetRequiredService<ILogger<ApnsPushSender>>()));
        else
            services.AddSingleton<IPushSender>(sp => new LoggingPushSender(DevicePlatform.iOS, sp.GetRequiredService<ILogger<LoggingPushSender>>()));

        return services;
    }
}
