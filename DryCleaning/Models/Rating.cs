namespace DryCleaning.Models;

/// <summary>A customer's rating of a provider: 1 to 5 stars and an optional comment.</summary>
public sealed class Rating
{
    public Guid Id { get; init; }
    public Guid OrderId { get; init; }
    public string OrderNumber { get; init; } = string.Empty;
    public int Stars { get; init; }
    public string? Comment { get; init; }
    public string CustomerName { get; init; } = string.Empty;
    public DateTime CreatedAt { get; init; }
}
