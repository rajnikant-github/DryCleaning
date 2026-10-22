namespace DryCleaning.Api.Common;

/// <summary>Section "Storage:Minio": used when Storage:Provider is "Minio" (same layout as the E Dry Cleaners reference project).</summary>
public sealed class MinioOptions
{
    /// <summary>MinIO address without a scheme, e.g. "localhost:9000" or "77.237.233.114:9000".</summary>
    public string Url { get; set; } = "localhost:9000";

    public string? AccessKey { get; set; }

    public string? SecretKey { get; set; }

    /// <summary>The one bucket that holds everything. Created on startup when missing. Use a bucket of its own: the API sets the bucket policy (public/* readable, nothing else).</summary>
    public string BucketName { get; set; } = "edrycleaners-api";

    /// <summary>True when MinIO is served over HTTPS.</summary>
    public bool UseSsl { get; set; }

    /// <summary>
    /// Public address of the bucket for logos, e.g. https://cdn.edrycleaners.co.za (a CDN or reverse proxy that points at the bucket).
    /// Leave empty to use MinIO's own address.
    /// </summary>
    public string? PublicUrl { get; set; }

    /// <summary>How long a signed order-photo link works.</summary>
    public int SignedLinkMinutes { get; set; } = 15;
}
