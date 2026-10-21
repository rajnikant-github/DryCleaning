namespace DryCleaning.Api.Dtos;

/// <summary>Whether the API can send email right now (never includes keys or passwords).</summary>
public sealed class EmailStatusDto
{
    public required bool IsConfigured { get; init; }

    /// <summary>"SendGrid", "SMTP" or "Not configured".</summary>
    public required string Provider { get; init; }

    public required string? From { get; init; }
}
