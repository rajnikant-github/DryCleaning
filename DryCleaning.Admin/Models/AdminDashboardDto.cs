namespace DryCleaning.Admin.Models;

public sealed class AdminDashboardDto
{
    public int TotalCustomers { get; set; }
    public int TotalProviders { get; set; }
    public int TotalDrivers { get; set; }
    public int ActiveOrders { get; set; }
    public int CompletedOrders { get; set; }
    public decimal Revenue { get; set; }
    public int OpenDisputes { get; set; }
    public string Currency { get; set; } = "ZAR";
    public Dictionary<string, int> OrdersByStatus { get; set; } = [];
    public List<DailyCountDto> OrdersLast30Days { get; set; } = [];
}
