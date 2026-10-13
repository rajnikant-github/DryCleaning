using DryCleaning.Api.Domain;

namespace DryCleaning.Api.Dtos;

public sealed class OrderPhotoDto
{
    public required Guid Id { get; init; }
    public required PhotoType Type { get; init; }
    public required string Url { get; init; }
    public required UploadedBy UploadedBy { get; init; }
    public required DateTime CreatedAt { get; init; }
}
