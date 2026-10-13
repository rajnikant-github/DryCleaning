using DryCleaning.Api.Domain;

namespace DryCleaning.Api.Dtos;

public sealed class RequestOtpRequest
{
    public string Identifier { get; set; } = string.Empty;
}
