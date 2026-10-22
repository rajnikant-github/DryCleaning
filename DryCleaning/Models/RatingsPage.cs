namespace DryCleaning.Models;

/// <summary>One page of a provider's ratings.</summary>
public sealed class RatingsPage
{
    public IReadOnlyList<Rating> Items { get; init; } = [];
    public bool HasMore { get; init; }
}
