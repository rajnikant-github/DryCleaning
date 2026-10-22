namespace DryCleaning.Api.Common;

/// <summary>The email address is the unique sign-in key, so profile updates may repeat it but never change it.</summary>
public static class EmailRules
{
    /// <summary>True when both addresses are empty or equal (ignoring case and surrounding spaces).</summary>
    public static bool IsSame(string? stored, string? requested) =>
        string.Equals(Normalize(stored), Normalize(requested), StringComparison.Ordinal);

    private static string Normalize(string? email) => (email ?? string.Empty).Trim().ToLowerInvariant();
}
