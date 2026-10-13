namespace DryCleaning.Api.Dtos;

/// <summary>Multipart form with one file (field name "File").</summary>
public sealed class UploadFileForm
{
    public IFormFile File { get; set; } = null!;
}
