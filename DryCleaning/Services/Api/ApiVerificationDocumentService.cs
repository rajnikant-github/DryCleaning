using DryCleaning.Models;
using DryCleaning.Services.Api.Contracts;

namespace DryCleaning.Services.Api;

public sealed class ApiVerificationDocumentService(ApiClient api) : IVerificationDocumentService
{
    public async Task<DocumentsOverview> GetAsync() =>
        Map(await api.GetAsync("api/me/documents", ApiJsonContext.Default.MyDocumentsDto));

    public async Task<Uri> GetPreviewLinkAsync(Guid typeId)
    {
        var link = await api.PostAsync($"api/me/documents/{typeId}/preview-link", ApiJsonContext.Default.PreviewLinkDto);
        return new Uri(link.Url);
    }

    public async Task<string> DownloadAsync(Guid typeId, string fileName)
    {
        // A fresh folder per download keeps the original file name (and its extension, which the viewer app needs) without clashes.
        var safeName = string.IsNullOrWhiteSpace(fileName) ? "document" : Path.GetFileName(fileName);
        var folder = Path.Combine(FileSystem.CacheDirectory, "my-documents", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(folder);
        var target = Path.Combine(folder, safeName);
        await api.DownloadFileAsync($"api/me/documents/{typeId}/file", target);
        return target;
    }

    public async Task<DocumentsOverview> SubmitAsync(Guid typeId, string filePath, string? number, DateTime? expiresOn)
    {
        var fields = new Dictionary<string, string>();
        if (!string.IsNullOrWhiteSpace(number))
            fields["Number"] = number.Trim();
        if (expiresOn is { } date)
            fields["ExpiresOn"] = date.ToString("yyyy-MM-dd", System.Globalization.CultureInfo.InvariantCulture);

        return Map(await api.UploadFileAsync($"api/me/documents/{typeId}", filePath, fields, ApiJsonContext.Default.MyDocumentsDto));
    }

    private static DocumentsOverview Map(MyDocumentsDto dto) => new()
    {
        Items = dto.Items.Select(d => new DocumentRequirement
        {
            TypeId = d.TypeId, Name = d.Name, Description = d.Description, IsRequired = d.IsRequired, NumberLabel = d.NumberLabel,
            HasExpiry = d.HasExpiry, State = d.State, Number = d.Number, ExpiresOn = d.ExpiresOn, FileName = d.FileName,
            RejectionMessage = d.RejectionMessage, SubmittedAt = d.SubmittedAt
        }).ToList(),
        RequiredCount = dto.RequiredCount,
        VerifiedRequiredCount = dto.VerifiedRequiredCount,
        ActionNeededCount = dto.ActionNeededCount,
        PendingRequiredCount = dto.PendingRequiredCount,
        AllRequiredVerified = dto.AllRequiredVerified,
        MaxPdfBytes = dto.MaxPdfBytes,
        MaxImageBytes = dto.MaxImageBytes
    };
}
