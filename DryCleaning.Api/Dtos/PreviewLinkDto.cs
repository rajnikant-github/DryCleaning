namespace DryCleaning.Api.Dtos;

/// <summary>A short-lived private link that opens one uploaded document in a browser.</summary>
public sealed class PreviewLinkDto
{
    public required string Url { get; init; }

    /// <summary>How long the link works.</summary>
    public required int ExpiresInSeconds { get; init; }
}
