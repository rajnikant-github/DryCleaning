namespace DryCleaning.Models;

public sealed class DriverProfile
{
    public required User User { get; init; }
    public required Driver Driver { get; init; }
    public Address? BaseAddress { get; init; }
    public int CompletedJobs { get; init; }
    public decimal TotalEarnings { get; init; }
}
