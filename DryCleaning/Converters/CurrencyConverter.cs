using System.Globalization;
using DryCleaning.Helpers;

namespace DryCleaning.Converters;

/// <summary>decimal → "R 1 234,56" using the configured currency.</summary>
public sealed class CurrencyConverter : IValueConverter
{
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture) => value switch
    {
        decimal amount => CurrencyFormatter.Current.Format(amount),
        double amount => CurrencyFormatter.Current.Format((decimal)amount),
        int amount => CurrencyFormatter.Current.Format(amount),
        _ => string.Empty
    };

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture) =>
        throw new NotSupportedException();
}
