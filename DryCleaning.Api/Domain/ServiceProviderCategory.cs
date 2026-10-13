namespace DryCleaning.Api.Domain;

public class ServiceProviderCategory : EntityBase
{
    public Guid ServiceProviderId { get; set; }
    public Guid CategoryId { get; set; }
    public bool IsActive { get; set; } = true;

    public Provider ServiceProvider { get; set; } = null!;
    public Category Category { get; set; } = null!;
}
