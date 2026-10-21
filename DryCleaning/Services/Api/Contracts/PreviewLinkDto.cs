namespace DryCleaning.Services.Api.Contracts;

public sealed class PreviewLinkDto
{
    public string Url { get; set; } = string.Empty;
    public int ExpiresInSeconds { get; set; }
}
