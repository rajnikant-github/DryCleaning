using DryCleaning.Api.Domain;
using Microsoft.EntityFrameworkCore;

namespace DryCleaning.Api.Common;

public sealed class StorageOptions
{
    public const string Section = "Storage";

    /// <summary>Folder under wwwroot where uploaded order photos are saved.</summary>
    public string UploadsFolder { get; set; } = "uploads";
    /// <summary>"Local" (wwwroot, development / single server) or "AzureBlob".</summary>
    public string Provider { get; set; } = "Local";

    public AzureBlobOptions AzureBlob { get; set; } = new();

    public bool UseAzureBlob => string.Equals(Provider, "AzureBlob", StringComparison.OrdinalIgnoreCase);

    public long MaxUploadBytes { get; set; } = 5 * 1024 * 1024;
}
