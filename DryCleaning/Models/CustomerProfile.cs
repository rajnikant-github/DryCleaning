namespace DryCleaning.Models;

public sealed class CustomerProfile
{
    public required User User { get; init; }
    public Address? Address { get; init; }
    public int OrderCount { get; init; }
}
