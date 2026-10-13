namespace DryCleaning.Models;

public sealed class ProviderListing
{
    public Guid Id { get; init; }
    public string BusinessName { get; init; } = string.Empty;
    public string? Description { get; init; }
    public string? Locality { get; init; }
    public IReadOnlyList<string> Categories { get; init; } = [];
    public decimal? StartingPrice { get; init; }
    public int? FastestDays { get; init; }
}
