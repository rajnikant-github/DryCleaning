using System.Globalization;
using DryCleaning.Configuration;

namespace DryCleaning.Helpers;

public interface ICurrencyFormatter
{
    string Code { get; }
    string Symbol { get; }
    string Format(decimal amount);
    bool TryParse(string? text, out decimal amount);
}

/// <summary>Formats money using the currency configured in appsettings.json.</summary>
public sealed class CurrencyFormatter : ICurrencyFormatter
{
    private readonly NumberFormatInfo _format;

    public CurrencyFormatter(CurrencySettings settings)
    {
        CultureInfo culture;
        try
        {
            culture = CultureInfo.GetCultureInfo(settings.Culture);
        }
        catch (CultureNotFoundException)
        {
            culture = CultureInfo.InvariantCulture;
        }

        _format = (NumberFormatInfo)culture.NumberFormat.Clone();
        _format.CurrencySymbol = settings.Symbol;
        _format.CurrencyDecimalDigits = settings.DecimalPlaces;
        Code = settings.Code;
        Symbol = settings.Symbol;
    }

    /// <summary>Used by XAML converters, which can't take constructor dependencies.</summary>
    public static ICurrencyFormatter Current { get; private set; } = new CurrencyFormatter(new CurrencySettings());

    public static void Initialize(ICurrencyFormatter formatter) => Current = formatter;

    public string Code { get; }
    public string Symbol { get; }

    public string Format(decimal amount) => amount.ToString("C", _format);

    public bool TryParse(string? text, out decimal amount)
    {
        amount = 0;
        if (string.IsNullOrWhiteSpace(text))
            return false;

        var cleaned = text.Replace(Symbol, string.Empty).Trim();
        return decimal.TryParse(cleaned, NumberStyles.Number, _format, out amount)
               || decimal.TryParse(cleaned, NumberStyles.Number, CultureInfo.InvariantCulture, out amount);
    }
}
