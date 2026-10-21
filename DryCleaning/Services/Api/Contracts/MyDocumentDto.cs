using DryCleaning.Models;

namespace DryCleaning.Services.Api.Contracts;

public sealed class MyDocumentDto
{
    public Guid TypeId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public bool IsRequired { get; set; }
    public string? NumberLabel { get; set; }
    public bool HasExpiry { get; set; }
    public DocumentState State { get; set; }
    public string? Number { get; set; }
    public DateTime? ExpiresOn { get; set; }
    public string? FileName { get; set; }
    public string? RejectionMessage { get; set; }
    public DateTime? SubmittedAt { get; set; }
}
