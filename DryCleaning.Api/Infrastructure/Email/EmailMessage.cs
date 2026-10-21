namespace DryCleaning.Api.Infrastructure.Email;

/// <summary>One email to one person. <see cref="Html"/> is the designed version, <see cref="Text"/> the plain fallback.</summary>
public sealed class EmailMessage
{
    public required string To { get; init; }
    public required string Subject { get; init; }
    public required string Html { get; init; }
    public required string Text { get; init; }
}
