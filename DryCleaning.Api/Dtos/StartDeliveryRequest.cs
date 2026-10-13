using DryCleaning.Api.Domain;

namespace DryCleaning.Api.Dtos;

/// <summary>D09 – collected from the provider.</summary>
public sealed class StartDeliveryRequest
{
    public int? PackageCount { get; set; }
    public string? Notes { get; set; }
}
