namespace DryCleaning.Models;

/// <summary>A photo or PDF chosen on the phone, ready to upload.</summary>
public sealed class PickedDocument
{
    public required string Path { get; init; }
    public required string FileName { get; init; }
}
