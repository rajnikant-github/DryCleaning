namespace DryCleaning.Api.Dtos;

public sealed class MyDocumentsDto
{
    public required IReadOnlyList<MyDocumentDto> Items { get; init; }

    /// <summary>How many documents are required.</summary>
    public required int RequiredCount { get; init; }

    /// <summary>Required documents an admin has verified.</summary>
    public required int VerifiedRequiredCount { get; init; }

    /// <summary>Required documents not uploaded yet or rejected: the applicant still has to act.</summary>
    public required int ActionNeededCount { get; init; }

    /// <summary>Required documents uploaded and waiting for an admin.</summary>
    public required int PendingRequiredCount { get; init; }

    public required bool AllRequiredVerified { get; init; }

    /// <summary>The largest PDF the server accepts (the admin sets it), so the app can check before uploading.</summary>
    public required long MaxPdfBytes { get; init; }

    /// <summary>The largest photo the server accepts. The app shrinks photos to fit.</summary>
    public required long MaxImageBytes { get; init; }
}
