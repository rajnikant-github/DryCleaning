namespace DryCleaning.Models;

public sealed class ProviderProfile
{
    public required Provider Provider { get; init; }
    public required User Owner { get; init; }
    public required Address Address { get; init; }
    public IReadOnlyList<Category> Categories { get; init; } = [];
}
