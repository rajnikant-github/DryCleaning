using DryCleaning.Models;

namespace DryCleaning.Helpers;

public sealed class ProviderAction
{
    public required string Label { get; init; }
    public required string Route { get; init; }
}
