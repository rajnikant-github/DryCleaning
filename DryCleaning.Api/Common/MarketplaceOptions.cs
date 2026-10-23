using DryCleaning.Api.Domain;
using Microsoft.EntityFrameworkCore;

namespace DryCleaning.Api.Common;

/// <summary>Section "Marketplace": currency, VAT and fees. Change values in appsettings.json.</summary>
public sealed class MarketplaceOptions
{
    public const string Section = "Marketplace";

    public string Currency { get; set; } = "ZAR";

    /// <summary>When true, providers only see/accept orders with a successful payment.</summary>
    public bool RequirePaymentBeforeAccept { get; set; } = true;
}
