namespace DryCleaning.Models;

public sealed class ServiceProviderCategory : EntityBase
{
    public Guid ServiceProviderId { get; set; }
    public Guid CategoryId { get; set; }
    public bool IsActive { get; set; } = true;
}
