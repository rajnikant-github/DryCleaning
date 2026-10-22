namespace DryCleaning.Api.Dtos;

public sealed class AdminRatingDto
{
    public required Guid Id { get; init; }
    public required Guid OrderId { get; init; }
    public required string OrderNumber { get; init; }
    public required string ProviderName { get; init; }
    public required string CustomerName { get; init; }
    public required int Stars { get; init; }

    /// <summary>The comment as written, even when hidden from the app.</summary>
    public required string? Comment { get; init; }
    public required bool CommentHidden { get; init; }
    public required DateTime CreatedAt { get; init; }
}
