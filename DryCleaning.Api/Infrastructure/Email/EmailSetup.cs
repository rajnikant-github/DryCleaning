using DryCleaning.Api.Common;

namespace DryCleaning.Api.Infrastructure.Email;

public static class EmailSetup
{
    /// <summary>Registers the queue, the sender (real when configured, log-only otherwise) and the background dispatcher.</summary>
    public static IServiceCollection AddEmail(this IServiceCollection services, IConfiguration config)
    {
        services.Configure<EmailOptions>(config.GetSection(EmailOptions.Section));
        services.AddSingleton<EmailQueue>();
        services.AddHostedService<EmailDispatcher>();

        var email = config.GetSection(EmailOptions.Section).Get<EmailOptions>() ?? new EmailOptions();
        if (email.Enabled && email.IsConfigured)
            services.AddSingleton<IEmailSender, SmtpEmailSender>();
        else
            services.AddSingleton<IEmailSender, LoggingEmailSender>();

        return services;
    }
}
