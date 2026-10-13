namespace DryCleaning.Helpers;

public static class ImageSources
{
    /// <summary>A web address (from the API) or a file path on the phone (demo data / just-picked photo).</summary>
    public static ImageSource? FromPathOrUrl(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return null;

        return Uri.TryCreate(value, UriKind.Absolute, out var uri) && uri.Scheme.StartsWith("http", StringComparison.OrdinalIgnoreCase)
            ? ImageSource.FromUri(uri)
            : ImageSource.FromFile(value);
    }
}
