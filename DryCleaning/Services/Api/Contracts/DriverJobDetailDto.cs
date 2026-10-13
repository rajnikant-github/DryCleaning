using DryCleaning.Models;

namespace DryCleaning.Services.Api.Contracts;

/// <summary>GET api/driver/jobs/{id}.</summary>
public sealed class DriverJobDetailDto
{
    public DriverJobDto Job { get; set; } = new();
    public DriverJobStep NextStep { get; set; }
    public bool IsMine { get; set; }
    public string CustomerName { get; set; } = string.Empty;
    public string? CustomerPhone { get; set; }
    public string ProviderName { get; set; } = string.Empty;
    public string? ProviderPhone { get; set; }
    public List<OrderItemDto> Items { get; set; } = [];
}
