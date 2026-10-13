using DryCleaning.Models;

namespace DryCleaning.Services.Api.Contracts;

/// <summary>GET api/driver/jobs/counts.</summary>
public sealed class DriverJobCountsDto
{
    public int Available { get; set; }
    public int Mine { get; set; }
    public int Completed { get; set; }
}
