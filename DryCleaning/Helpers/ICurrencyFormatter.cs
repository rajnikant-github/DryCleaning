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
