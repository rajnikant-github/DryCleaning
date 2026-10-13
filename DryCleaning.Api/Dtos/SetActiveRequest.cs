using DryCleaning.Api.Domain;

namespace DryCleaning.Api.Dtos;

public sealed class SetActiveRequest
{
    public bool IsActive { get; set; }
}
