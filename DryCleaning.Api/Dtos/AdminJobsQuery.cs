using DryCleaning.Api.Domain;

namespace DryCleaning.Api.Dtos;

public sealed class AdminJobsQuery : Common.PagedRequest
{
    public DriverJobStatus? Status { get; set; }
    public DriverJobType? JobType { get; set; }
}
