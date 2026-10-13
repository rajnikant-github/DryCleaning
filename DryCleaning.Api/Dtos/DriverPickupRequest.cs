using DryCleaning.Api.Domain;

namespace DryCleaning.Api.Dtos;

/// <summary>D06 – pickup code plus the item check. <c>ItemCount</c> is kept for older clients.</summary>
public sealed class DriverPickupRequest
{
    public string Code { get; set; } = string.Empty;
    public IReadOnlyList<DriverItemCheckRequest>? Items { get; set; }
    public int? ItemCount { get; set; }
    public string? Notes { get; set; }
}
