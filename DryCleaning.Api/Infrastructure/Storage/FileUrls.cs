namespace DryCleaning.Api.Infrastructure.Storage;

/// <summary>
/// Turns stored paths into client URLs. The mapping code is static, so it can't take the storage service by injection;
/// the service is registered once at start-up instead.
/// </summary>
public static class FileUrls
{
    private static IFileStorage? _storage;

    public static void Use(IFileStorage storage) => _storage = storage;

    /// <summary>The URL a client can open for a stored path (unchanged when it isn't one we manage).</summary>
    public static string? Resolve(string? storedPath) =>
        string.IsNullOrWhiteSpace(storedPath) ? storedPath : _storage?.GetReadUrl(storedPath) ?? storedPath;
}
