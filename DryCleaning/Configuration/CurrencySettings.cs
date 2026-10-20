using System.Text.Json;
using System.Text.Json.Serialization;

namespace DryCleaning.Configuration;

public sealed class CurrencySettings
{
    /// <summary>ISO 4217 code, e.g. ZAR.</summary>
    public string Code { get; set; } = "ZAR";

    /// <summary>Symbol shown in the UI, e.g. R.</summary>
    public string Symbol { get; set; } = "R";

    /// <summary>Culture used for number grouping/decimal separators. en-US gives R1,234.50; en-ZA would give R1 234,50.</summary>
    public string Culture { get; set; } = "en-US";

    public int DecimalPlaces { get; set; } = 2;
}
