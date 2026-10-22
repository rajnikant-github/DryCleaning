using DryCleaning.Api.Common;
using Microsoft.Extensions.Options;
using Minio;
using Minio.DataModel.Args;

namespace DryCleaning.Api.Infrastructure.Storage;

/// <summary>
/// MinIO (S3-compatible) storage with a single bucket, like the reference project. Files sit under a folder per visibility:
/// <c>public/</c> (logos, anonymous read), <c>private/</c> (order photos, short-lived signed links) and <c>protected/</c>
/// (verification documents: no link ever, the API streams them after checking who is asking).
/// The database keeps "minio://public/logos/abc.png" – never a URL – so links are created on every request.
/// On startup the bucket is created if missing and only <c>public/*</c> is opened for anonymous read.
/// </summary>
public sealed class MinioFileStorage : IFileStorage
{
    private const string Scheme = "minio://";

    private readonly IMinioClient _client;
    private readonly MinioOptions _options;
    private readonly ILogger<MinioFileStorage> _logger;
    private readonly SemaphoreSlim _bucketGate = new(1, 1);
    private bool _bucketReady;

    public MinioFileStorage(IOptions<StorageOptions> options, ILogger<MinioFileStorage> logger)
    {
        _options = options.Value.Minio;
        _logger = logger;

        if (string.IsNullOrWhiteSpace(_options.AccessKey) || string.IsNullOrWhiteSpace(_options.SecretKey))
            throw new InvalidOperationException("File storage needs the MinIO keys: set Storage:Minio:AccessKey and Storage:Minio:SecretKey in secrets.json (or the Storage__Minio__AccessKey / Storage__Minio__SecretKey environment variables).");

        _client = new MinioClient()
            .WithEndpoint(_options.Url)
            .WithCredentials(_options.AccessKey, _options.SecretKey)
            .WithSSL(_options.UseSsl)
            .Build();
    }

    public async Task<string> SaveAsync(Stream content, string extension, string folder, FileVisibility visibility, CancellationToken ct)
    {
        await EnsureBucketAsync(ct);

        var key = $"{Prefix(visibility)}/{folder.Trim('/')}/{Guid.NewGuid():N}{extension}".Replace('\\', '/');
        var length = content.CanSeek ? content.Length - content.Position : -1;

        // MinIO needs the size up front; buffer when the stream cannot tell it.
        if (length < 0)
        {
            var buffer = new MemoryStream();
            await content.CopyToAsync(buffer, ct);
            buffer.Position = 0;
            content = buffer;
            length = buffer.Length;
        }

        await _client.PutObjectAsync(new PutObjectArgs()
            .WithBucket(_options.BucketName)
            .WithObject(key)
            .WithStreamData(content)
            .WithObjectSize(length)
            .WithContentType(ContentTypeFor(extension)), ct);

        return Scheme + key;
    }

    public string GetReadUrl(string storedPath)
    {
        if (!storedPath.StartsWith(Scheme, StringComparison.Ordinal))
            return storedPath; // an older local "/uploads/..." path

        var key = storedPath[Scheme.Length..];

        // Protected documents never get a link: the API streams them after checking who is asking.
        if (key.StartsWith("protected/", StringComparison.Ordinal))
            return storedPath;

        if (key.StartsWith("public/", StringComparison.Ordinal))
        {
            var baseUrl = !string.IsNullOrWhiteSpace(_options.PublicUrl)
                ? _options.PublicUrl.TrimEnd('/')
                : $"{(_options.UseSsl ? "https" : "http")}://{_options.Url}/{_options.BucketName}";
            return $"{baseUrl}/{key}";
        }

        return _client.PresignedGetObjectAsync(new PresignedGetObjectArgs()
            .WithBucket(_options.BucketName)
            .WithObject(key)
            .WithExpiry(_options.SignedLinkMinutes * 60)).GetAwaiter().GetResult();
    }

    public async Task<(Stream Content, string ContentType)> OpenReadAsync(string storedPath, CancellationToken ct)
    {
        var key = KeyFor(storedPath) ?? throw new FileNotFoundException("The file was not found.");

        try
        {
            var stat = await _client.StatObjectAsync(new StatObjectArgs().WithBucket(_options.BucketName).WithObject(key), ct);
            var buffer = new MemoryStream();
            await _client.GetObjectAsync(new GetObjectArgs()
                .WithBucket(_options.BucketName)
                .WithObject(key)
                .WithCallbackStream((stream, token) => stream.CopyToAsync(buffer, token)), ct);
            buffer.Position = 0;
            return (buffer, string.IsNullOrWhiteSpace(stat.ContentType) ? "application/octet-stream" : stat.ContentType);
        }
        catch (Minio.Exceptions.ObjectNotFoundException)
        {
            throw new FileNotFoundException("The file was not found.");
        }
    }

    public async Task DeleteAsync(string storedPath, CancellationToken ct)
    {
        if (KeyFor(storedPath) is { } key)
            await _client.RemoveObjectAsync(new RemoveObjectArgs().WithBucket(_options.BucketName).WithObject(key), ct);
    }

    private static string? KeyFor(string storedPath) =>
        storedPath.StartsWith(Scheme, StringComparison.Ordinal) ? storedPath[Scheme.Length..] : null;

    private static string Prefix(FileVisibility visibility) => visibility switch
    {
        FileVisibility.Public => "public",
        FileVisibility.Protected => "protected",
        _ => "private"
    };

    private async Task EnsureBucketAsync(CancellationToken ct)
    {
        if (_bucketReady)
            return;

        await _bucketGate.WaitAsync(ct);
        try
        {
            if (_bucketReady)
                return;

            var bucket = _options.BucketName;
            if (!await _client.BucketExistsAsync(new BucketExistsArgs().WithBucket(bucket), ct))
            {
                await _client.MakeBucketAsync(new MakeBucketArgs().WithBucket(bucket), ct);
                _logger.LogInformation("Created MinIO bucket {Bucket}.", bucket);
            }

            // Only public/* is readable without signing in; private/ and protected/ stay closed.
            var policy = $$"""
                {"Version":"2012-10-17","Statement":[{"Effect":"Allow","Principal":{"AWS":["*"]},"Action":["s3:GetObject"],"Resource":["arn:aws:s3:::{{bucket}}/public/*"]}]}
                """;
            await _client.SetPolicyAsync(new SetPolicyArgs().WithBucket(bucket).WithPolicy(policy), ct);

            _bucketReady = true;
        }
        finally
        {
            _bucketGate.Release();
        }
    }

    private static string ContentTypeFor(string extension) => extension.ToLowerInvariant() switch
    {
        ".pdf" => "application/pdf",
        ".png" => "image/png",
        ".heic" => "image/heic",
        ".webp" => "image/webp",
        _ => "image/jpeg"
    };
}
