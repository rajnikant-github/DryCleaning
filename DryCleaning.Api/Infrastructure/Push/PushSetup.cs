using DryCleaning.Api.Common;
using DryCleaning.Api.Domain;
using Google.Apis.Auth.OAuth2;

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

        // Android – Firebase Cloud Messaging (credentials pasted into secrets.json, or a file path)
        var firebase = FirebaseCredential(push);
        if (push.Enabled && firebase is not null)
            services.AddSingleton<IPushSender>(sp => new FcmPushSender(firebase, sp.GetRequiredService<ILogger<FcmPushSender>>()));
        else
            services.AddSingleton<IPushSender>(sp => new LoggingPushSender(DevicePlatform.Android, sp.GetRequiredService<ILogger<LoggingPushSender>>()));

        // iOS – Apple Push Notification service (the .p8 key pasted into secrets.json, or a file path)
        var apns = config.GetSection(ApnsOptions.Section).Get<ApnsOptions>() ?? new ApnsOptions();
        var apnsKey = apns.IsConfigured ? ApnsKey(apns, environment) : null;
        if (push.Enabled && apnsKey is not null)
            services.AddSingleton<IPushSender>(sp =>
                new ApnsPushSender(apns, apnsKey, handler: null, sp.GetRequiredService<ILogger<ApnsPushSender>>()));
        else
            services.AddSingleton<IPushSender>(sp => new LoggingPushSender(DevicePlatform.iOS, sp.GetRequiredService<ILogger<LoggingPushSender>>()));

        return services;
    }

    private static GoogleCredential? FirebaseCredential(PushOptions push)
    {
        if (!string.IsNullOrWhiteSpace(push.FirebaseCredentialsJson))
            return GoogleCredential.FromJson(push.FirebaseCredentialsJson);

        var path = push.FirebaseCredentialsPath;
        return !string.IsNullOrWhiteSpace(path) && File.Exists(path) ? GoogleCredential.FromFile(path) : null;
    }

    private static string? ApnsKey(ApnsOptions apns, IHostEnvironment environment)
    {
        if (!string.IsNullOrWhiteSpace(apns.PrivateKey))
            return apns.PrivateKey;

        var path = Path.GetFullPath(apns.PrivateKeyPath!, environment.ContentRootPath);
        return File.Exists(path) ? File.ReadAllText(path) : null;
    }
}
