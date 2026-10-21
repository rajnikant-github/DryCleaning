namespace DryCleaning.Api.Infrastructure.Email;

/// <summary>Used when email isn't configured: the email is only written to the log (without its body).</summary>
public sealed class LoggingEmailSender(ILogger<LoggingEmailSender> logger) : IEmailSender
{
    public Task SendAsync(EmailMessage message, CancellationToken ct)
    {
        logger.LogInformation("Email (not sent – email not configured) to {To}: {Subject}", message.To, message.Subject);
        return Task.CompletedTask;
    }
}
