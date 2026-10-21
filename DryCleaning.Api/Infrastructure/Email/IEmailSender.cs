namespace DryCleaning.Api.Infrastructure.Email;

public interface IEmailSender
{
    /// <summary>Sends one email. Throws when the mail server refuses it (the dispatcher logs and retries).</summary>
    Task SendAsync(EmailMessage message, CancellationToken ct);
}
