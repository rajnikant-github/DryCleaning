namespace DryCleaning.Api.Dtos;

public sealed class EmailTestResultDto
{
    /// <summary>How many emails were sent.</summary>
    public required int Sent { get; init; }
}
