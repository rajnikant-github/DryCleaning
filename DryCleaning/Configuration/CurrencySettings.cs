using System.Text.Json;
using System.Text.Json.Serialization;

namespace DryCleaning.Configuration;

public sealed class CurrencySettings
{
    /// <summary>ISO 4217 code, e.g. ZAR.</summary>
    public string Code { get; set; } = "ZAR";

    /// <summary>Symbol shown in the UI, e.g. R.</summary>
    public string Symbol { get; set; } = "R";

    /// <summary>Culture used for number grouping/decimal separators, e.g. en-ZA.</summary>
    public string Culture { get; set; } = "en-ZA";

    public int DecimalPlaces { get; set; } = 2;
}
