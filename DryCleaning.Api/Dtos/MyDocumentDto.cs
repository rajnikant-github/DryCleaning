namespace DryCleaning.Api.Dtos;

/// <summary>One requested document and the applicant's own state for it.</summary>
public sealed class MyDocumentDto
{
    public required Guid TypeId { get; init; }
    public required string Name { get; init; }
    public required string? Description { get; init; }
    public required bool IsRequired { get; init; }
    public required string? NumberLabel { get; init; }
    public required bool HasExpiry { get; init; }
    public required DocumentState State { get; init; }
    public required string? Number { get; init; }
    public required DateTime? ExpiresOn { get; init; }
    public required string? FileName { get; init; }

    /// <summary>The admin's message when <see cref="State"/> is Rejected.</summary>
    public required string? RejectionMessage { get; init; }

    public required DateTime? SubmittedAt { get; init; }
}
