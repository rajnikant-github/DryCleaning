using DryCleaning.Api.Domain;

namespace DryCleaning.Api.Dtos;

public sealed class DriverJobsQuery : Common.PagedRequest
{
    /// <summary>Optional filter: Completed or Cancelled for history.</summary>
    public DriverJobStatus? Status { get; set; }
}
