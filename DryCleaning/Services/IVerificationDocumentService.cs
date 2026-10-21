using DryCleaning.Models;

namespace DryCleaning.Services;

/// <summary>A provider's or driver's verification documents: what the admin asks for, and uploading them.</summary>
public interface IVerificationDocumentService
{
    Task<DocumentsOverview> GetAsync();

    /// <summary>A short-lived private link that opens my uploaded file in a web browser (e.g. a PDF preview).</summary>
    Task<Uri> GetPreviewLinkAsync(Guid typeId);

    /// <summary>Downloads the file I uploaded for this document to the phone and returns its local path, so it can be opened.</summary>
    Task<string> DownloadAsync(Guid typeId, string fileName);

    /// <summary>Uploads (or replaces) one document. A new file always goes back to review.</summary>
    Task<DocumentsOverview> SubmitAsync(Guid typeId, string filePath, string? number, DateTime? expiresOn);
}
