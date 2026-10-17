using Azure.Identity;
using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using Azure.Storage.Sas;
using DryCleaning.Api.Common;
using Microsoft.Extensions.Options;

namespace DryCleaning.Api.Infrastructure.Storage;

/// <summary>
/// Azure Blob Storage. Order photos go to a private container and are opened through short-lived signed links;
/// provider logos go to a public-read container. The database keeps "blob://container/path" – never a URL –
/// so links can be re-signed on every request.
/// </summary>
public sealed class AzureBlobFileStorage : IFileStorage
{
    private const string Scheme = "blob://";

    private readonly BlobServiceClient _service;
    private readonly AzureBlobOptions _options;
    private readonly SemaphoreSlim _containerGate = new(1, 1);
    private readonly object _keyGate = new();
    private bool _containersReady;
    private UserDelegationKey? _delegationKey;

    public AzureBlobFileStorage(IOptions<StorageOptions> options)
    {
        _options = options.Value.AzureBlob;
        _service = !string.IsNullOrWhiteSpace(_options.ConnectionString)
            ? new BlobServiceClient(_options.ConnectionString)
            : new BlobServiceClient(
                new Uri(_options.AccountUrl ?? throw new InvalidOperationException("Storage:AzureBlob needs AccountUrl or ConnectionString.")),
                new DefaultAzureCredential());
    }

    public async Task<string> SaveAsync(Stream content, string extension, string folder, FileVisibility visibility, CancellationToken ct)
    {
        await EnsureContainersAsync(ct);

        var container = visibility == FileVisibility.Public ? _options.PublicContainer : _options.PrivateContainer;
        var blobName = $"{folder.Trim('/')}/{Guid.NewGuid():N}{extension}".Replace('\\', '/');
        var blob = _service.GetBlobContainerClient(container).GetBlobClient(blobName);

        await blob.UploadAsync(content, new BlobUploadOptions
        {
            HttpHeaders = new BlobHttpHeaders { ContentType = ContentTypeFor(extension) }
        }, ct);

        return $"{Scheme}{container}/{blobName}";
    }

    public string GetReadUrl(string storedPath)
    {
        if (!storedPath.StartsWith(Scheme, StringComparison.Ordinal))
            return storedPath; // an older local "/uploads/..." path

        var rest = storedPath[Scheme.Length..];
        var slash = rest.IndexOf('/');
        if (slash <= 0)
            return storedPath;

        var container = rest[..slash];
        var blob = _service.GetBlobContainerClient(container).GetBlobClient(rest[(slash + 1)..]);

        return container == _options.PublicContainer ? blob.Uri.ToString() : SignedUri(blob).ToString();
    }

    private Uri SignedUri(BlobClient blob)
    {
        var expires = DateTimeOffset.UtcNow.AddMinutes(_options.SignedLinkMinutes);

        // Connection string / account key (Azurite, local testing): sign directly.
        if (blob.CanGenerateSasUri)
            return blob.GenerateSasUri(BlobSasPermissions.Read, expires);

        // Managed identity: sign with a user delegation key (reused until it is close to expiring).
        var key = DelegationKey();
        var sas = new BlobSasBuilder(BlobSasPermissions.Read, expires)
        {
            BlobContainerName = blob.BlobContainerName,
            BlobName = blob.Name,
            Resource = "b"
        };
        return new BlobUriBuilder(blob.Uri) { Sas = sas.ToSasQueryParameters(key, _service.AccountName) }.ToUri();
    }

    private UserDelegationKey DelegationKey()
    {
        lock (_keyGate)
        {
            if (_delegationKey is null || _delegationKey.SignedExpiresOn < DateTimeOffset.UtcNow.AddMinutes(10))
                _delegationKey = _service.GetUserDelegationKey(new BlobGetUserDelegationKeyOptions(DateTimeOffset.UtcNow.AddHours(1)) { StartsOn = DateTimeOffset.UtcNow.AddMinutes(-5) }).Value;
            return _delegationKey;
        }
    }

    private async Task EnsureContainersAsync(CancellationToken ct)
    {
        if (_containersReady)
            return;

        await _containerGate.WaitAsync(ct);
        try
        {
            if (_containersReady)
                return;
            await _service.GetBlobContainerClient(_options.PrivateContainer).CreateIfNotExistsAsync(PublicAccessType.None, cancellationToken: ct);
            await _service.GetBlobContainerClient(_options.PublicContainer).CreateIfNotExistsAsync(PublicAccessType.Blob, cancellationToken: ct);
            _containersReady = true;
        }
        finally
        {
            _containerGate.Release();
        }
    }

    private static string ContentTypeFor(string extension) => extension.ToLowerInvariant() switch
    {
        ".png" => "image/png",
        ".heic" => "image/heic",
        ".webp" => "image/webp",
        _ => "image/jpeg"
    };
}
