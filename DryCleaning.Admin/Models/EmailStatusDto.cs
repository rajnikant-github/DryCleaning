namespace DryCleaning.Admin.Models;

public sealed class EmailStatusDto
{
    public bool IsConfigured { get; set; }
    public string Provider { get; set; } = string.Empty;
    public string? From { get; set; }
}
