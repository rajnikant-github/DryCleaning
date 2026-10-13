namespace DryCleaning.Models;

public sealed class Customer : EntityBase
{
    public Guid UserId { get; set; }
    public Guid AddressId { get; set; }
}
