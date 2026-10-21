namespace DryCleaning.Services.Api.Contracts;

public sealed class MyDocumentsDto
{
    public List<MyDocumentDto> Items { get; set; } = [];
    public int RequiredCount { get; set; }
    public int VerifiedRequiredCount { get; set; }
    public int ActionNeededCount { get; set; }
    public int PendingRequiredCount { get; set; }
    public bool AllRequiredVerified { get; set; }
    public long MaxPdfBytes { get; set; } = 50 * 1024 * 1024;
    public long MaxImageBytes { get; set; } = 15 * 1024 * 1024;
}
