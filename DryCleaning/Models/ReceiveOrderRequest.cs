namespace DryCleaning.Models;

public sealed class ReceiveOrderRequest
{
    public int ActualItemCount { get; init; }
    public string? Notes { get; init; }
    public IReadOnlyList<string> PhotoPaths { get; init; } = [];
}
