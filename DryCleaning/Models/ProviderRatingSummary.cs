namespace DryCleaning.Models;

/// <summary>A provider's average stars and the newest comments.</summary>
public sealed class ProviderRatingSummary
{
    public double? Average { get; init; }
    public int Count { get; init; }
    public IReadOnlyList<Rating> Latest { get; init; } = [];
}
