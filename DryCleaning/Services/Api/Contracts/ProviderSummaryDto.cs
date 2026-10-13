using DryCleaning.Models;

namespace DryCleaning.Services.Api.Contracts;

/// <summary>Provider card in the customer catalog.</summary>
public sealed class ProviderSummaryDto
{
    public Guid Id { get; set; }
    public string BusinessName { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string? LogoUrl { get; set; }
    public string? Locality { get; set; }
    public List<string> Categories { get; set; } = [];
    public decimal? StartingPrice { get; set; }
    public int? FastestDays { get; set; }
}
