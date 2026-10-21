namespace DryCleaning.Helpers;

/// <summary>Wording for the VAT line, using the rate the server charged (a percentage such as 15).</summary>
public static class VatText
{
    public static string Label(decimal ratePercent) =>
        ratePercent > 0 ? $"VAT ({ratePercent.ToString("0.##", System.Globalization.CultureInfo.InvariantCulture)}%)" : "VAT";
}
