namespace DryCleaning.Api.Common;

/// <summary>
/// Section "Email": how the API sends email. Prefers SendGrid (set <see cref="Key"/> to a key starting with "SG."),
/// otherwise a normal SMTP login (<see cref="SmtpServer"/>, <see cref="Username"/>, <see cref="Password"/> – e.g. Office 365).
/// Keep the key and password in secrets.json, never in appsettings.json.
/// </summary>
public sealed class EmailOptions
{
    public const string Section = "Email";

    public bool Enabled { get; set; } = true;

    /// <summary>SendGrid API key ("SG.…"). When set, mail goes through smtp.sendgrid.net.</summary>
    public string? Key { get; set; }

    public string SmtpServer { get; set; } = "smtp.office365.com";
    public int Port { get; set; } = 587;
    public string? Username { get; set; }
    public string? Password { get; set; }

    /// <summary>Sender address (must be verified in SendGrid, or be the mailbox you log in with).</summary>
    public string? From { get; set; }

    /// <summary>Sender name people see, e.g. "E Dry Cleaners".</summary>
    public string Name { get; set; } = "E Dry Cleaners";

    /// <summary>Where replies go (optional), e.g. a support mailbox.</summary>
    public string? ReplyTo { get; set; }

    public bool UsesSendGrid => !string.IsNullOrWhiteSpace(Key) && Key.StartsWith("SG.", StringComparison.Ordinal);

    public bool IsConfigured =>
        !string.IsNullOrWhiteSpace(From) &&
        (UsesSendGrid || (!string.IsNullOrWhiteSpace(SmtpServer) && !string.IsNullOrWhiteSpace(Username) && !string.IsNullOrWhiteSpace(Password)));
}
