using DryCleaning.Models;

namespace DryCleaning.Services.Api.Contracts;

/// <summary>GET api/driver/dashboard.</summary>
public sealed class DriverDashboardDto
{
    public string DriverName { get; set; } = string.Empty;
    public bool IsAvailable { get; set; }
    public int AvailableJobs { get; set; }
    public DriverJobDetailDto? ActiveJob { get; set; }
    public int CompletedToday { get; set; }
    public decimal EarningsToday { get; set; }
    public int UnreadNotifications { get; set; }
    public List<DriverJobDto> TodaysJobs { get; set; } = [];
    public string Currency { get; set; } = string.Empty;
}
