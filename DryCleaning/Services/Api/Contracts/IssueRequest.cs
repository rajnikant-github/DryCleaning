using DryCleaning.Models;

namespace DryCleaning.Services.Api.Contracts;

/// <summary>Dispute or issue report.</summary>
public sealed class IssueRequest
{
    public string Reason { get; set; } = string.Empty;
    public string? Description { get; set; }
}
