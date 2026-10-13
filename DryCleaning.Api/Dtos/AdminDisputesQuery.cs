using DryCleaning.Api.Domain;

namespace DryCleaning.Api.Dtos;

public sealed class AdminDisputesQuery : Common.PagedRequest
{
    public DisputeStatus? Status { get; set; }
}
