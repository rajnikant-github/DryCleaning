namespace DryCleaning.Api.Common;

/// <summary>Section "Storage": every file (photos, logos, verification documents) lives in MinIO – there is no other store.</summary>
public sealed class StorageOptions
{
    public const string Section = "Storage";

    public MinioOptions Minio { get; set; } = new();

    public long MaxUploadBytes { get; set; } = 5 * 1024 * 1024;
}
