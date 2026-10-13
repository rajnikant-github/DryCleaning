using Android.App;
using Android.Content;
using Android.Content.PM;

namespace DryCleaning;

/// <summary>Receives edrycleaners://payment-complete after Paystack checkout and hands it to WebAuthenticator.</summary>
[Activity(NoHistory = true, LaunchMode = LaunchMode.SingleTop, Exported = true)]
[IntentFilter(
    new[] { Intent.ActionView },
    Categories = new[] { Intent.CategoryDefault, Intent.CategoryBrowsable },
    DataScheme = "edrycleaners",
    DataHost = "payment-complete")]
public sealed class WebAuthenticationCallbackActivity : Microsoft.Maui.Authentication.WebAuthenticatorCallbackActivity
{
}
