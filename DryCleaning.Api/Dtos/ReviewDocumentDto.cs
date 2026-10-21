namespace DryCleaning.Api.Dtos;

/// <summary>One document row on the admin's review page (uploaded or not).</summary>
public sealed class ReviewDocumentDto
{
    /// <summary>Null when nothing was uploaded for this type.</summary>
    public required Guid? DocumentId { get; init; }

    public required Guid TypeId { get; init; }
    public required string Name { get; init; }
    public required bool IsRequired { get; init; }

    /// <summary>False when the admin switched this document type off: the upload is still shown but doesn't count.</summary>
    public required bool TypeActive { get; init; }
    public required string? NumberLabel { get; init; }
    public required bool HasExpiry { get; init; }
    public required DocumentState State { get; init; }
    public required string? Number { get; init; }
    public required DateTime? ExpiresOn { get; init; }
    public required string? FileName { get; init; }
    public required string? ContentType { get; init; }
    public required long? SizeBytes { get; init; }
    public required DateTime? SubmittedAt { get; init; }
    public required DateTime? ReviewedAt { get; init; }
    public required string? RejectionMessage { get; init; }
    public required IReadOnlyList<DocumentEventDto> History { get; init; }
}
