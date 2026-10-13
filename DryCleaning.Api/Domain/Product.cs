namespace DryCleaning.Api.Domain;

public class Product : EntityBase
{
    public Guid ServiceProviderId { get; set; }
    public Guid CategoryId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string? ImageUrl { get; set; }
    public int DisplayOrder { get; set; }
    public bool IsActive { get; set; } = true;

    public Provider ServiceProvider { get; set; } = null!;
    public Category Category { get; set; } = null!;
    public ICollection<ServiceItem> ServiceItems { get; set; } = [];
}
