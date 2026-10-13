using DryCleaning.Models;

namespace DryCleaning.Services.Api.Contracts;

/// <summary>POST api/driver/jobs/{id}/pickup.</summary>
public sealed class DriverPickupRequest
{
    public string Code { get; set; } = string.Empty;
    public List<DriverItemCheckRequest>? Items { get; set; }
    public int? ItemCount { get; set; }
    public string? Notes { get; set; }
}
