using DryCleaning.Api.Domain;

namespace DryCleaning.Api.Dtos;

public sealed class AssignDriverRequest
{
    public Guid DriverId { get; set; }
}
