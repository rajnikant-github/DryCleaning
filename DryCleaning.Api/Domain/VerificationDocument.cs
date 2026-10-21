namespace DryCleaning.Api.Domain;

/// <summary>
/// One applicant's current upload for one document type. Uploading again replaces the file and sends it back to review.
/// The file itself is in protected storage; only a stored path is kept here.
/// </summary>
public class VerificationDocument : EntityBase
{
    public Guid UserId { get; set; }
    public Guid DocumentTypeId { get; set; }

    /// <summary>Path in protected storage – never turned into a link.</summary>
    public string FilePath { get; set; } = string.Empty;

    public string FileName { get; set; } = string.Empty;
    public string ContentType { get; set; } = string.Empty;
    public long SizeBytes { get; set; }

    /// <summary>The number the applicant typed (licence number, registration number, …).</summary>
    public string? Number { get; set; }

    public DateTime? ExpiresOn { get; set; }
    public DocumentStatus Status { get; set; } = DocumentStatus.Pending;

    /// <summary>Why an admin rejected it; shown to the applicant.</summary>
    public string? RejectionMessage { get; set; }

    public Guid? ReviewedByUserId { get; set; }
    public DateTime? ReviewedAt { get; set; }
    public DateTime SubmittedAt { get; set; } = DateTime.UtcNow;

    public User User { get; set; } = null!;
    public DocumentType DocumentType { get; set; } = null!;
    public List<DocumentReviewEvent> Events { get; set; } = [];
}
