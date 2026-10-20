namespace DryCleaning.Api.Domain;

/// <summary>Maps to the ServiceProviders table (class named Provider to avoid clashing with DI's ServiceProvider).</summary>
public class Provider : EntityBase
{
    public Guid UserId { get; set; }
    public Guid? AddressId { get; set; }
    public string BusinessName { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string? Phone { get; set; }
    public string? Email { get; set; }
    public string? LogoUrl { get; set; }

    /// <summary>False until approved by an admin.</summary>
    public bool IsActive { get; set; }

    public User User { get; set; } = null!;
    public Address? Address { get; set; }
    public ICollection<ServiceProviderCategory> Categories { get; set; } = [];
    public ICollection<ProviderWorkingHour> WorkingHours { get; set; } = [];
    public ICollection<Product> Products { get; set; } = [];
    public ICollection<Order> Orders { get; set; } = [];
}
