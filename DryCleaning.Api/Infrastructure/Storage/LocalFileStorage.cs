using DryCleaning.Api.Common;
using Microsoft.AspNetCore.StaticFiles;
using Microsoft.Extensions.Options;

namespace DryCleaning.Api.Infrastructure.Storage;

/// <summary>
/// Stores files on this server – for development and single-server hosting. Public and private files go under wwwroot and are
/// served as static files (visibility is not enforced). <see cref="FileVisibility.Protected"/> files go under
/// App_Data/protected, outside wwwroot, so they can only be read through the API.
/// Use <see cref="AzureBlobFileStorage"/> in Azure.
/// </summary>
public sealed class LocalFileStorage(IWebHostEnvironment environment, IOptions<StorageOptions> options) : IFileStorage
{
    private const string ProtectedScheme = "protected://";
    private static readonly FileExtensionContentTypeProvider ContentTypes = new();

    private string WebRoot => environment.WebRootPath ?? Path.Combine(environment.ContentRootPath, "wwwroot");
    private string ProtectedRoot => Path.Combine(environment.ContentRootPath, "App_Data", "protected");

    public async Task<string> SaveAsync(Stream content, string extension, string folder, FileVisibility visibility, CancellationToken ct)
    {
        var fileName = $"{Guid.NewGuid():N}{extension}";

        if (visibility == FileVisibility.Protected)
        {
            var protectedFolder = Path.Combine(ProtectedRoot, folder);
            Directory.CreateDirectory(protectedFolder);
            await using (var file = File.Create(Path.Combine(protectedFolder, fileName)))
                await content.CopyToAsync(file, ct);
            return ProtectedScheme + $"{folder.Trim('/', '\\')}/{fileName}".Replace('\\', '/');
        }

        var relativeFolder = Path.Combine(options.Value.UploadsFolder, folder);
        var absoluteFolder = Path.Combine(WebRoot, relativeFolder);
        Directory.CreateDirectory(absoluteFolder);
        await using (var file = File.Create(Path.Combine(absoluteFolder, fileName)))
            await content.CopyToAsync(file, ct);
        return "/" + Path.Combine(relativeFolder, fileName).Replace('\\', '/');
    }

    public string GetReadUrl(string storedPath) => storedPath;

    public Task<(Stream Content, string ContentType)> OpenReadAsync(string storedPath, CancellationToken ct)
    {
        var path = ResolvePath(storedPath) ?? throw new FileNotFoundException("The file was not found.");
        if (!File.Exists(path))
            throw new FileNotFoundException("The file was not found.");

        var contentType = ContentTypes.TryGetContentType(path, out var type) ? type : "application/octet-stream";
        return Task.FromResult<(Stream, string)>((File.OpenRead(path), contentType));
    }

    public Task DeleteAsync(string storedPath, CancellationToken ct)
    {
        if (ResolvePath(storedPath) is { } path && File.Exists(path))
            File.Delete(path);
        return Task.CompletedTask;
    }

    /// <summary>Maps a stored path to a file on disk, refusing anything that would leave the storage folders.</summary>
    private string? ResolvePath(string storedPath)
    {
        var (root, relative) = storedPath.StartsWith(ProtectedScheme, StringComparison.Ordinal)
            ? (ProtectedRoot, storedPath[ProtectedScheme.Length..])
            : (WebRoot, storedPath.TrimStart('/'));

        var full = Path.GetFullPath(Path.Combine(root, relative.Replace('/', Path.DirectorySeparatorChar)));
        return full.StartsWith(Path.GetFullPath(root) + Path.DirectorySeparatorChar, StringComparison.Ordinal) ? full : null;
    }
}
