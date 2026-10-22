namespace DryCleaning.Services.Api.Contracts;

/// <summary>A customer's rating of a provider.</summary>
public sealed class RatingDto
{
    public Guid Id { get; set; }
    public Guid OrderId { get; set; }
    public string OrderNumber { get; set; } = string.Empty;
    public int Stars { get; set; }
    public string? Comment { get; set; }
    public string CustomerName { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
}
