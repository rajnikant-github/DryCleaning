using DryCleaning.Api.Common;
using MailKit.Net.Smtp;
using MailKit.Security;
using Microsoft.Extensions.Options;
using MimeKit;

namespace DryCleaning.Api.Infrastructure.Email;

/// <summary>Sends through SendGrid's SMTP relay (when a "SG." key is set) or through the configured SMTP server.</summary>
public sealed class SmtpEmailSender(IOptions<EmailOptions> options) : IEmailSender
{
    public async Task SendAsync(EmailMessage message, CancellationToken ct)
    {
        var settings = options.Value;
        var (host, user, password) = settings.UsesSendGrid
            ? ("smtp.sendgrid.net", "apikey", settings.Key!)
            : (settings.SmtpServer, settings.Username!, settings.Password!);

        var mime = new MimeMessage();
        mime.From.Add(new MailboxAddress(settings.Name, settings.From!));
        if (!string.IsNullOrWhiteSpace(settings.ReplyTo))
            mime.ReplyTo.Add(MailboxAddress.Parse(settings.ReplyTo));
        mime.To.Add(MailboxAddress.Parse(message.To));
        mime.Subject = message.Subject;
        mime.Body = new BodyBuilder { HtmlBody = message.Html, TextBody = message.Text }.ToMessageBody();

        using var client = new SmtpClient { Timeout = 20_000 };
        await client.ConnectAsync(host, settings.Port > 0 ? settings.Port : 587, SecureSocketOptions.StartTls, ct);
        await client.AuthenticateAsync(user, password, ct);
        await client.SendAsync(mime, ct);
        await client.DisconnectAsync(true, ct);
    }
}
