namespace DryCleaning.Api.Dtos;

public sealed class RejectDocumentRequest
{
    /// <summary>Why it was rejected. The applicant sees this next to the document.</summary>
    public string Message { get; set; } = string.Empty;
}
