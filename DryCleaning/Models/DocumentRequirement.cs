namespace DryCleaning.Models;

/// <summary>A document the admin asks for, and the applicant's own state for it.</summary>
public sealed class DocumentRequirement
{
    public Guid TypeId { get; init; }
    public string Name { get; init; } = string.Empty;
    public string? Description { get; init; }
    public bool IsRequired { get; init; }

    /// <summary>E.g. "Licence number"; null when the document has no number.</summary>
    public string? NumberLabel { get; init; }

    public bool HasExpiry { get; init; }
    public DocumentState State { get; init; }
    public string? Number { get; init; }
    public DateTime? ExpiresOn { get; init; }
    public string? FileName { get; init; }

    /// <summary>The admin's message when the document was rejected.</summary>
    public string? RejectionMessage { get; init; }

    public DateTime? SubmittedAt { get; init; }
}
