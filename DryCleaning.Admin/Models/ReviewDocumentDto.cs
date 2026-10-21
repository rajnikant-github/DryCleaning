namespace DryCleaning.Admin.Models;

public sealed class ReviewDocumentDto
{
    public Guid? DocumentId { get; set; }
    public Guid TypeId { get; set; }
    public string Name { get; set; } = string.Empty;
    public bool IsRequired { get; set; }
    public bool TypeActive { get; set; } = true;
    public string? NumberLabel { get; set; }
    public bool HasExpiry { get; set; }
    public DocumentState State { get; set; }
    public string? Number { get; set; }
    public DateTime? ExpiresOn { get; set; }
    public string? FileName { get; set; }
    public string? ContentType { get; set; }
    public long? SizeBytes { get; set; }
    public DateTime? SubmittedAt { get; set; }
    public DateTime? ReviewedAt { get; set; }
    public string? RejectionMessage { get; set; }
    public List<DocumentEventDto> History { get; set; } = [];
}
