namespace DryCleaning.Models;

/// <summary>A Google Places autocomplete suggestion.</summary>
public sealed class PlacePrediction
{
    public string PlaceId { get; init; } = string.Empty;
    public string MainText { get; init; } = string.Empty;
    public string SecondaryText { get; init; } = string.Empty;
}
