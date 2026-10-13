using DryCleaning.Models;

namespace DryCleaning.Services.Api.Contracts;

/// <summary>GET api/driver/earnings.</summary>
public sealed class DriverEarningsDto
{
    public EarningsPeriod Period { get; set; }
    public DateTime From { get; set; }
    public DateTime To { get; set; }
    public decimal Total { get; set; }
    public int JobCount { get; set; }
    public int PickupCount { get; set; }
    public int DeliveryCount { get; set; }
    public decimal RatePerJob { get; set; }
    public decimal AveragePerJob { get; set; }
    public string Currency { get; set; } = string.Empty;
    public List<EarningsBarDto> Bars { get; set; } = [];
    public List<DriverJobDto> Jobs { get; set; } = [];
}
