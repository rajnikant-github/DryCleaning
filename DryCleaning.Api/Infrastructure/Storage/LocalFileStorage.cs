using DryCleaning.Api.Common;
using Microsoft.Extensions.Options;

namespace DryCleaning.Api.Infrastructure.Storage;

public interface IFileStorage
{
    /// <summary>Saves the stream and returns a public relative URL (e.g. /uploads/orders/…/x.jpg).</summary>
    Task<string> SaveAsync(Stream content, string extension, string folder, CancellationToken ct);
}

/// <summary>
/// Stores files under wwwroot so they're served as static files. Swap for Azure Blob / S3 in production
/// by implementing <see cref="IFileStorage"/>.
/// </summary>
public sealed class LocalFileStorage(IWebHostEnvironment environment, IOptions<StorageOptions> options) : IFileStorage
{
    public async Task<string> SaveAsync(Stream content, string extension, string folder, CancellationToken ct)
    {
        var webRoot = environment.WebRootPath ?? Path.Combine(environment.ContentRootPath, "wwwroot");
        var relativeFolder = Path.Combine(options.Value.UploadsFolder, folder);
        var absoluteFolder = Path.Combine(webRoot, relativeFolder);
        Directory.CreateDirectory(absoluteFolder);

        var fileName = $"{Guid.NewGuid():N}{extension}";
        var path = Path.Combine(absoluteFolder, fileName);
        await using (var file = File.Create(path))
            await content.CopyToAsync(file, ct);

        return "/" + Path.Combine(relativeFolder, fileName).Replace('\\', '/');
    }
}
