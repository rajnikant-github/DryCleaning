using DryCleaning.Models;

namespace DryCleaning.Services.Api.Contracts;

/// <summary>RFC 7807 error body returned by the API.</summary>
public sealed class ProblemDetailsDto
{
    public string? Title { get; set; }
    public string? Detail { get; set; }
    public int? Status { get; set; }
    public Dictionary<string, string[]>? Errors { get; set; }
}
