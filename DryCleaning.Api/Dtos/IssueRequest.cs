using DryCleaning.Api.Domain;

namespace DryCleaning.Api.Dtos;

public sealed class IssueRequest
{
    public string Reason { get; set; } = string.Empty;
    public string? Description { get; set; }
}
