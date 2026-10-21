namespace DryCleaning.Helpers;

public static class SizeText
{
    /// <summary>E.g. "50 MB", or "2.5 MB" when it isn't a whole number.</summary>
    public static string Megabytes(long bytes)
    {
        var mb = bytes / (1024.0 * 1024.0);
        return mb >= 1 ? $"{mb:0.#} MB" : $"{bytes / 1024} KB";
    }
}
