namespace DryCleaning.Api.Infrastructure.Storage;

public interface IFileStorage
{
    /// <summary>Saves the stream and returns the stored path to keep in the database (not necessarily a URL).</summary>
    Task<string> SaveAsync(Stream content, string extension, string folder, FileVisibility visibility, CancellationToken ct);

    /// <summary>Turns a stored path into a URL a client can open (signed and short-lived for private files).</summary>
    string GetReadUrl(string storedPath);
}
