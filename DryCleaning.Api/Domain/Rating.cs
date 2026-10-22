namespace DryCleaning.Api.Domain;

/// <summary>A customer's rating of the service provider after a completed order. One per order; it can't be changed.</summary>
public class Rating : EntityBase
{
    public Guid OrderId { get; set; }
    public Guid CustomerId { get; set; }
    public Guid ServiceProviderId { get; set; }

    /// <summary>1 (poor) to 5 (excellent).</summary>
    public int Stars { get; set; }

    public string? Comment { get; set; }

    /// <summary>An admin can hide an abusive comment: the stars still count, the comment is no longer shown.</summary>
    public bool CommentHidden { get; set; }

    public Order Order { get; set; } = null!;
    public Customer Customer { get; set; } = null!;
    public Provider ServiceProvider { get; set; } = null!;
}
