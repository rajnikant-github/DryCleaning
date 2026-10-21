namespace DryCleaning.Api.Dtos;

public sealed class TestEmailRequest
{
    public string To { get; set; } = string.Empty;

    /// <summary>True = send one sample of every email template (welcome, codes, receipt, announcement) instead of the single test email.</summary>
    public bool AllTemplates { get; set; }
}
