using DryCleaning.Api.Domain;
using Microsoft.EntityFrameworkCore;

namespace DryCleaning.Api.Common;

public sealed class StorageOptions
{
    public const string Section = "Storage";

    /// <summary>Folder under wwwroot where uploaded order photos are saved.</summary>
    public string UploadsFolder { get; set; } = "uploads";
    public long MaxUploadBytes { get; set; } = 5 * 1024 * 1024;
}
