using System.Globalization;

namespace DryCleaning.Admin.Services;

/// <summary>Display formatting shared by the pages.</summary>
public static class Fmt
{
    private static readonly CultureInfo Za = new("en-ZA");

    public static string Money(decimal amount, string currency = "ZAR") =>
        currency == "ZAR" ? $"R{amount.ToString("N2", Za)}" : $"{amount.ToString("N2", Za)} {currency}";

    public static string Date(DateTime? value) => value is { } d ? d.ToLocalTime().ToString("d MMM yyyy", Za) : "–";

    public static string DateTime(DateTime? value) => value is { } d ? d.ToLocalTime().ToString("d MMM yyyy HH:mm", Za) : "–";

    /// <summary>"ReadyForDelivery" → "Ready for delivery".</summary>
    public static string Words(object value)
    {
        var text = value.ToString() ?? string.Empty;
        var spaced = System.Text.RegularExpressions.Regex.Replace(text, "(?<=[a-z])(?=[A-Z])", " ");
        return spaced.Length == 0 ? spaced : char.ToUpper(spaced[0]) + spaced[1..].ToLower();
    }
}
