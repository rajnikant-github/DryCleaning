namespace DryCleaning.Models;

public sealed class DocumentsOverview
{
    public IReadOnlyList<DocumentRequirement> Items { get; init; } = [];
    public int RequiredCount { get; init; }
    public int VerifiedRequiredCount { get; init; }

    /// <summary>Required documents not uploaded yet or rejected.</summary>
    public int ActionNeededCount { get; init; }

    /// <summary>Required documents waiting for an admin.</summary>
    public int PendingRequiredCount { get; init; }

    public bool AllRequiredVerified { get; init; }

    /// <summary>The largest PDF the server accepts (set by the admin).</summary>
    public long MaxPdfBytes { get; init; } = 50 * 1024 * 1024;

    /// <summary>The largest photo the server accepts; the app shrinks photos to fit.</summary>
    public long MaxImageBytes { get; init; } = 15 * 1024 * 1024;
}
