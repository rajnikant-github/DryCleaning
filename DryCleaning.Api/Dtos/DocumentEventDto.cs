using DryCleaning.Api.Domain;

namespace DryCleaning.Api.Dtos;

public sealed class DocumentEventDto
{
    public required DocumentAction Action { get; init; }
    public required string ActorName { get; init; }
    public required string? Message { get; init; }
    public required DateTime CreatedAt { get; init; }
}
