namespace DryCleaning.Models;

/// <summary>A marketplace category and whether the signed-in provider offers it.</summary>
public sealed class ProviderCategoryOption
{
    public Guid CategoryId { get; init; }
    public string Name { get; init; } = string.Empty;
    public string Description { get; init; } = string.Empty;
    public int DisplayOrder { get; init; }
    public bool IsEnabled { get; init; }
}
