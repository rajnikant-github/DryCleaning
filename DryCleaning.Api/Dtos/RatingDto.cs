namespace DryCleaning.Api.Dtos;

public sealed class RatingDto
{
    public required Guid Id { get; init; }
    public required Guid OrderId { get; init; }
    public required string OrderNumber { get; init; }
    public required int Stars { get; init; }

    /// <summary>The comment, or null when there is none or an admin hid it.</summary>
    public required string? Comment { get; init; }

    /// <summary>The customer's first name only.</summary>
    public required string CustomerName { get; init; }
    public required DateTime CreatedAt { get; init; }
}
