using DryCleaning.Api.Common;
using Microsoft.Extensions.Options;

namespace DryCleaning.Api.Infrastructure.Storage;

/// <summary>
/// Stores files under wwwroot so they're served as static files – for development and single-server hosting.
/// Visibility is ignored: every file is reachable by its link. Use <see cref="AzureBlobFileStorage"/> in Azure.
/// </summary>
public sealed class LocalFileStorage(IWebHostEnvironment environment, IOptions<StorageOptions> options) : IFileStorage
{
    public async Task<string> SaveAsync(Stream content, string extension, string folder, FileVisibility visibility, CancellationToken ct)
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

    public string GetReadUrl(string storedPath) => storedPath;
}
