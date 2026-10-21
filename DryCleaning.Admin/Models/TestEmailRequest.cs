namespace DryCleaning.Admin.Models;

public sealed class TestEmailRequest
{
    public string To { get; set; } = string.Empty;
    public bool AllTemplates { get; set; }
}
