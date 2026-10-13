using DryCleaning.Models;

namespace DryCleaning.Services.Api.Contracts;

/// <summary>POST api/driver/jobs/{id}/start-delivery.</summary>
public sealed class StartDeliveryRequest
{
    public int? PackageCount { get; set; }
    public string? Notes { get; set; }
}
