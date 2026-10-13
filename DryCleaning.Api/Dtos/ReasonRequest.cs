using DryCleaning.Api.Domain;

namespace DryCleaning.Api.Dtos;

public sealed class ReasonRequest
{
    public string Reason { get; set; } = string.Empty;
}
