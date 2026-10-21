namespace DryCleaning.Api.Domain;

/// <summary>Audit trail entry: who submitted, verified, rejected or opened a document, and when.</summary>
public class DocumentReviewEvent : EntityBase
{
    public Guid VerificationDocumentId { get; set; }
    public DocumentAction Action { get; set; }

    /// <summary>The applicant (for Submitted) or the admin (for the rest).</summary>
    public Guid ActorUserId { get; set; }

    /// <summary>The rejection message, for Rejected.</summary>
    public string? Message { get; set; }

    public VerificationDocument VerificationDocument { get; set; } = null!;
}
