using DryCleaning.Models;

namespace DryCleaning.Services.Api.Contracts;

/// <summary>GET api/customer/orders/counts.</summary>
public sealed class CustomerOrderCountsDto
{
    public int Active { get; set; }
    public int Completed { get; set; }
    public int Cancelled { get; set; }
}
