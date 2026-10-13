namespace DryCleaning.Models;

public sealed class DriverPickupRequest
{
    public string PickupCode { get; init; } = string.Empty;
    public IReadOnlyList<DriverItemCheck> Items { get; init; } = [];
    public IReadOnlyList<string> PhotoPaths { get; init; } = [];
    public string? Notes { get; init; }
}
