using DryCleaning.Models;

namespace DryCleaning.Services.Mock;

/// <summary>Demo data: a few documents; uploading one marks it as waiting for review.</summary>
public sealed class MockVerificationDocumentService : IVerificationDocumentService
{
    private readonly List<DocumentRequirement> _items =
    [
        new() { TypeId = Guid.NewGuid(), Name = "ID document or passport", IsRequired = true, NumberLabel = "ID / passport number", State = DocumentState.NotSubmitted },
        new() { TypeId = Guid.NewGuid(), Name = "Proof of address", IsRequired = true, State = DocumentState.NotSubmitted },
        new() { TypeId = Guid.NewGuid(), Name = "Tax clearance", IsRequired = false, NumberLabel = "Tax number", HasExpiry = true, State = DocumentState.NotSubmitted }
    ];

    private readonly Dictionary<Guid, string> _files = [];

    public async Task<DocumentsOverview> GetAsync()
    {
        await MockDataStore.SimulateLatencyAsync();
        return Overview();
    }

    public async Task<DocumentsOverview> SubmitAsync(Guid typeId, string filePath, string? number, DateTime? expiresOn)
    {
        await MockDataStore.SimulateLatencyAsync();
        _files[typeId] = filePath;
        var index = _items.FindIndex(i => i.TypeId == typeId);
        if (index >= 0)
        {
            var old = _items[index];
            _items[index] = new DocumentRequirement
            {
                TypeId = old.TypeId, Name = old.Name, Description = old.Description, IsRequired = old.IsRequired, NumberLabel = old.NumberLabel,
                HasExpiry = old.HasExpiry, State = DocumentState.Pending, Number = number, ExpiresOn = expiresOn,
                FileName = Path.GetFileName(filePath), SubmittedAt = DateTime.Now
            };
        }

        return Overview();
    }

    public Task<Uri> GetPreviewLinkAsync(Guid typeId) =>
        throw new NotSupportedException("Browser preview isn't available in demo mode.");

    public Task<string> DownloadAsync(Guid typeId, string fileName) =>
        _files.TryGetValue(typeId, out var path) && File.Exists(path)
            ? Task.FromResult(path)
            : throw new InvalidOperationException("The file isn't available in demo mode.");

    private DocumentsOverview Overview()
    {
        var required = _items.Where(i => i.IsRequired).ToList();
        return new DocumentsOverview
        {
            Items = _items.ToList(),
            RequiredCount = required.Count,
            VerifiedRequiredCount = required.Count(i => i.State == DocumentState.Verified),
            ActionNeededCount = required.Count(i => i.State is DocumentState.NotSubmitted or DocumentState.Rejected),
            PendingRequiredCount = required.Count(i => i.State == DocumentState.Pending),
            AllRequiredVerified = required.All(i => i.State == DocumentState.Verified),
            MaxPdfBytes = 50 * 1024 * 1024,
            MaxImageBytes = 15 * 1024 * 1024
        };
    }
}
