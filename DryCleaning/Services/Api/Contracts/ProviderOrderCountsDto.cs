using DryCleaning.Models;

namespace DryCleaning.Services.Api.Contracts;

/// <summary>GET api/provider/orders/counts.</summary>
public sealed class ProviderOrderCountsDto
{
    public int New { get; set; }
    public int Incoming { get; set; }
    public int InProcess { get; set; }
    public int Ready { get; set; }
    public int Completed { get; set; }
    public int Cancelled { get; set; }
    public int Disputed { get; set; }
}
