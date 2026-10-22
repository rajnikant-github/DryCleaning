namespace DryCleaning.Admin.Models;

public sealed class AdminRatingDto
{
    public Guid Id { get; set; }
    public Guid OrderId { get; set; }
    public string OrderNumber { get; set; } = string.Empty;
    public string ProviderName { get; set; } = string.Empty;
    public string CustomerName { get; set; } = string.Empty;
    public int Stars { get; set; }
    public string? Comment { get; set; }
    public bool CommentHidden { get; set; }
    public DateTime CreatedAt { get; set; }
}
