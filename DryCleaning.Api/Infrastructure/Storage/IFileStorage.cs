namespace DryCleaning.Api.Infrastructure.Storage;

public interface IFileStorage
{
    /// <summary>Saves the stream and returns the stored path to keep in the database (not necessarily a URL).</summary>
    Task<string> SaveAsync(Stream content, string extension, string folder, FileVisibility visibility, CancellationToken ct);

    /// <summary>Turns a stored path into a URL a client can open (signed and short-lived for private files).</summary>
    string GetReadUrl(string storedPath);

    /// <summary>Opens a stored file for streaming through the API (used for <see cref="FileVisibility.Protected"/> files).</summary>
    Task<(Stream Content, string ContentType)> OpenReadAsync(string storedPath, CancellationToken ct);

    /// <summary>Deletes a stored file. Does nothing when it is already gone.</summary>
    Task DeleteAsync(string storedPath, CancellationToken ct);
}
