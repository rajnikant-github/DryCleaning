namespace DryCleaning.Api.Domain;

public class Customer : EntityBase
{
    public Guid UserId { get; set; }
    public Guid? AddressId { get; set; }

    public User User { get; set; } = null!;
    public Address? Address { get; set; }
    public ICollection<Order> Orders { get; set; } = [];
}
