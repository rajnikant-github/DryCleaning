using DryCleaning.Models;

namespace DryCleaning.Services.Api.Contracts;

/// <summary>GET api/provider/dashboard.</summary>
public sealed class ProviderDashboardDto
{
    public string BusinessName { get; set; } = string.Empty;
    public int NewCount { get; set; }
    public int IncomingCount { get; set; }
    public int InProcessCount { get; set; }
    public int ReadyCount { get; set; }
    public int CompletedCount { get; set; }
    public int UnreadNotifications { get; set; }
    public List<OrderSummaryDto> TodaysOrders { get; set; } = [];
    public List<ActivityDto> RecentActivity { get; set; } = [];
}
