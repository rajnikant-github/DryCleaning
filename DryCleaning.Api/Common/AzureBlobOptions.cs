namespace DryCleaning.Api.Common;

/// <summary>Section "Storage:AzureBlob": used when Storage:Provider is "AzureBlob".</summary>
public sealed class AzureBlobOptions
{
    /// <summary>
    /// Storage account URL, e.g. https://edrycleaners.blob.core.windows.net. The API signs in with its managed identity
    /// (role: Storage Blob Data Contributor). Leave empty when <see cref="ConnectionString"/> is set.
    /// </summary>
    public string? AccountUrl { get; set; }

    /// <summary>Connection string (local Azurite: UseDevelopmentStorage=true). Keep real keys in Key Vault / App Settings.</summary>
    public string? ConnectionString { get; set; }

    /// <summary>Private container for order photos.</summary>
    public string PrivateContainer { get; set; } = "order-photos";

    /// <summary>Public-read container for provider logos.</summary>
    public string PublicContainer { get; set; } = "public-assets";

    /// <summary>How long a signed photo link works.</summary>
    public int SignedLinkMinutes { get; set; } = 15;
}
