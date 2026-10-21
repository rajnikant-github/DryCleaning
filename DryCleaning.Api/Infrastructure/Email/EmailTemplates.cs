using System.Globalization;
using System.Net;
using System.Text;
using DryCleaning.Api.Domain;

namespace DryCleaning.Api.Infrastructure.Email;

/// <summary>
/// The emails the API sends. Every email shares one branded layout and carries a plain-text version.
/// Anything that came from a person (names, notes, announcement text) is HTML-encoded before it goes into the page.
/// </summary>
public static class EmailTemplates
{
    // Brand taken from edrycleaners.co.za: coral accent, near-black text, dark footer, Nunito.
    private const string Coral = "#F24A4A";
    private const string Ink = "#1A1A2E";
    private const string AppName = "E Dry Cleaners";
    private const string SiteUrl = "https://edrycleaners.co.za";

    private static readonly CultureInfo Numbers = new("en-US");

    /// <summary>Sign-in or password-reset code.</summary>
    public static EmailMessage Code(string to, string? name, string code, int validMinutes, bool passwordReset)
    {
        var purpose = passwordReset ? "reset your password" : "sign in";
        var title = passwordReset ? "Reset your password" : "Your sign-in code";
        var body = $"""
            <p style="margin:0 0 16px">{Greeting(name)}</p>
            <p style="margin:0 0 20px">Use this code to {purpose}. It is valid for {validMinutes} minutes.</p>
            <p style="margin:0 0 20px;text-align:center">
              <span style="display:inline-block;padding:14px 28px;border-radius:12px;background:#FFF0F0;color:{Coral};font-size:32px;font-weight:700;letter-spacing:8px">{Enc(code)}</span>
            </p>
            <p style="margin:0;color:#6B7280;font-size:13px">If you didn't ask for this, you can ignore this email. Never share this code with anyone.</p>
            """;
        var text = $"{GreetingText(name)}\n\nUse this code to {purpose}: {code}\nIt is valid for {validMinutes} minutes.\n\nIf you didn't ask for this, you can ignore this email. Never share this code with anyone.";
        return Build(to, $"{code} is your {AppName} code", title, body, text);
    }

    /// <summary>The code that confirms a new sign-up's email address (the account is created once it is entered).</summary>
    public static EmailMessage SignUpCode(string to, string? name, string code, int validMinutes)
    {
        var body = $"""
            <p style="margin:0 0 16px">{Greeting(name)}</p>
            <p style="margin:0 0 20px">Use this code to confirm your email address and finish creating your account. It is valid for {validMinutes} minutes.</p>
            <p style="margin:0 0 20px;text-align:center">
              <span style="display:inline-block;padding:14px 28px;border-radius:12px;background:#FFF0F0;color:{Coral};font-size:32px;font-weight:700;letter-spacing:8px">{Enc(code)}</span>
            </p>
            <p style="margin:0;color:#6B7280;font-size:13px">If you didn't try to sign up, you can ignore this email. Never share this code with anyone.</p>
            """;
        var text = $"{GreetingText(name)}\n\nUse this code to confirm your email and finish creating your account: {code}\nIt is valid for {validMinutes} minutes.\n\nIf you didn't try to sign up, you can ignore this email.";
        return Build(to, $"{code} is your {AppName} sign-up code", "Confirm your email", body, text);
    }

    /// <summary>Sent once after sign-up.</summary>
    public static EmailMessage Welcome(string to, string? name, UserRole role)
    {
        var (line, next) = role switch
        {
            UserRole.Provider => ("Thanks for registering your business.", "Our team will review it and let you know as soon as it's approved. After that you can sign in, add your services and start receiving orders."),
            UserRole.Driver => ("Thanks for signing up as a driver.", "Our team will review your account and let you know as soon as it's approved. After that, go online in the app to see pickup and delivery jobs near you."),
            _ => ("Thanks for signing up.", "Choose a provider, schedule a pickup and we'll collect, clean and deliver your clothes back to your door.")
        };
        var body = $"""
            <p style="margin:0 0 16px">{Greeting(name)}</p>
            <p style="margin:0 0 12px;font-size:18px;font-weight:700;color:{Coral}">Welcome to {AppName}!</p>
            <p style="margin:0 0 12px">{Enc(line)}</p>
            <p style="margin:0">{Enc(next)}</p>
            """;
        var text = $"{GreetingText(name)}\n\nWelcome to {AppName}! {line}\n\n{next}";
        return Build(to, $"Welcome to {AppName}", "Welcome", body, text);
    }

    /// <summary>Receipt after a successful payment.</summary>
    public static EmailMessage OrderConfirmation(string to, string? name, Order order, string providerName, string currency)
    {
        var rows = new StringBuilder();
        var textRows = new StringBuilder();
        foreach (var item in order.Items)
        {
            var label = $"{item.Quantity} × {item.Product?.Name ?? "Item"} – {item.ServiceItem?.Name ?? ""}".TrimEnd(' ', '–');
            rows.Append($"<tr><td style=\"padding:6px 0\">{Enc(label)}</td><td style=\"padding:6px 0;text-align:right\">{Enc(Money(item.TotalPrice, currency))}</td></tr>");
            textRows.AppendLine($"  {label}  {Money(item.TotalPrice, currency)}");
        }

        var vatLabel = order.VatRate > 0 ? $"VAT ({order.VatRate:0.##}%)" : "VAT";
        var extraRows = new StringBuilder();
        var extraText = new StringBuilder();
        foreach (var charge in order.ExtraCharges.OrderBy(c => c.DisplayOrder))
        {
            extraRows.Append($"<tr><td style=\"padding:3px 0;color:#6B7280\">{Enc(charge.Name + (string.IsNullOrEmpty(charge.Basis) ? "" : $" ({charge.Basis})"))}</td><td style=\"padding:3px 0;text-align:right\">{Enc(Money(charge.Amount, currency))}</td></tr>");
            extraText.AppendLine($"{charge.Name}{(string.IsNullOrEmpty(charge.Basis) ? "" : $" ({charge.Basis})")}: {Money(charge.Amount, currency)}");
        }

        var pickup = order.PickupDate is { } date
            ? $"{date.ToString("dddd, d MMMM yyyy", Numbers)}{(string.IsNullOrWhiteSpace(order.PickupSlot) ? "" : $" · {order.PickupSlot}")}"
            : "To be arranged";

        var body = $"""
            <p style="margin:0 0 16px">{Greeting(name)}</p>
            <p style="margin:0 0 4px;font-size:18px;font-weight:700;color:{Coral}">Payment received – thank you!</p>
            <p style="margin:0 0 20px">Order <strong>{Enc(order.OrderNumber)}</strong> with {Enc(providerName)} is confirmed.</p>
            <table role="presentation" width="100%" cellspacing="0" cellpadding="0" style="border-top:1px solid #E5E2D8;border-bottom:1px solid #E5E2D8;margin:0 0 16px">
              {rows}
            </table>
            <table role="presentation" width="100%" cellspacing="0" cellpadding="0" style="margin:0 0 20px">
              <tr><td style="padding:3px 0;color:#6B7280">Subtotal</td><td style="padding:3px 0;text-align:right">{Enc(Money(order.SubTotal, currency))}</td></tr>
              <tr><td style="padding:3px 0;color:#6B7280">Pickup &amp; delivery</td><td style="padding:3px 0;text-align:right">{Enc(Money(order.DeliveryFee, currency))}</td></tr>
              {extraRows}
              <tr><td style="padding:3px 0;color:#6B7280">{Enc(vatLabel)}</td><td style="padding:3px 0;text-align:right">{Enc(Money(order.Tax, currency))}</td></tr>
              <tr><td style="padding:8px 0 0;font-weight:700">Total paid</td><td style="padding:8px 0 0;text-align:right;font-weight:700;color:{Coral}">{Enc(Money(order.TotalAmount, currency))}</td></tr>
            </table>
            <p style="margin:0 0 4px;color:#6B7280">Pickup</p>
            <p style="margin:0 0 20px">{Enc(pickup)}</p>
            <p style="margin:0;color:#6B7280;font-size:13px">You can follow your order and share your pickup and delivery codes with the driver in the app.</p>
            """;
        var text = $"{GreetingText(name)}\n\nPayment received – thank you! Order {order.OrderNumber} with {providerName} is confirmed.\n\n{textRows}\nSubtotal: {Money(order.SubTotal, currency)}\nPickup & delivery: {Money(order.DeliveryFee, currency)}\n{extraText}{vatLabel}: {Money(order.Tax, currency)}\nTotal paid: {Money(order.TotalAmount, currency)}\n\nPickup: {pickup}\n\nYou can follow your order and share your pickup and delivery codes with the driver in the app.";
        return Build(to, $"Order {order.OrderNumber} confirmed – receipt", "Order confirmed", body, text);
    }

    /// <summary>An announcement an admin chose to also send by email.</summary>
    public static EmailMessage Announcement(string to, string? name, string title, string message)
    {
        var body = $"""
            <p style="margin:0 0 16px">{Greeting(name)}</p>
            <p style="margin:0 0 12px;font-size:18px;font-weight:700;color:{Coral}">{Enc(title)}</p>
            <p style="margin:0;white-space:pre-line">{Enc(message)}</p>
            """;
        return Build(to, title, title, body, $"{GreetingText(name)}\n\n{title}\n\n{message}");
    }

    /// <summary>Sent by the admin "send test email" action.</summary>
    public static EmailMessage Test(string to)
    {
        var body = $"""
            <p style="margin:0 0 12px;font-size:18px;font-weight:700;color:{Coral}">Email is working</p>
            <p style="margin:0">This is a test email from the {AppName} admin portal. If you can read this, the API can send email.</p>
            """;
        return Build(to, $"{AppName} test email", "Test email", body, $"Email is working. This is a test email from the {AppName} admin portal.");
    }

    public static string Money(decimal amount, string currency) =>
        currency == "ZAR" ? $"R{amount.ToString("N2", Numbers)}" : $"{amount.ToString("N2", Numbers)} {currency}";

    private static EmailMessage Build(string to, string subject, string heading, string bodyHtml, string text) => new()
    {
        To = to,
        Subject = subject,
        Html = Layout(heading, bodyHtml),
        Text = text + $"\n\n— {AppName}\n{SiteUrl}\nTerms: {SiteUrl}/terms.html · Contact: {SiteUrl}/contact.html\nPowered by Macdroid Software Solutions"
    };

    private static string Layout(string heading, string body) => $"""
        <!doctype html>
        <html lang="en">
        <head><meta charset="utf-8"><meta name="viewport" content="width=device-width,initial-scale=1"><title>{Enc(heading)}</title></head>
        <body style="margin:0;padding:0;background:#F8F7F2;font-family:'Nunito','Segoe UI',Arial,sans-serif;color:#111827;font-size:15px;line-height:1.5">
          <table role="presentation" width="100%" cellspacing="0" cellpadding="0" style="background:#F8F7F2;padding:24px 12px">
            <tr><td align="center">
              <table role="presentation" width="100%" cellspacing="0" cellpadding="0" style="max-width:560px;background:#FFFFFF;border-radius:16px;overflow:hidden;border:1px solid #E5E2D8">
                <tr><td style="padding:20px 28px;border-bottom:4px solid {Coral}">
                  <table role="presentation" cellspacing="0" cellpadding="0"><tr>
                    <td style="padding-right:12px"><a href="{SiteUrl}"><img src="{SiteUrl}/assets/images/logo.png" width="44" height="44" alt="" style="display:block;border:0;border-radius:22px"></a></td>
                    <td style="font-size:22px;font-weight:800;color:{Ink};letter-spacing:.2px"><a href="{SiteUrl}" style="color:{Ink};text-decoration:none">{AppName}</a></td>
                  </tr></table>
                </td></tr>
                <tr><td style="padding:28px">{body}</td></tr>
                <tr><td style="padding:20px 28px;background:#0F0A1E;color:#C9C7D6;font-size:12px;line-height:1.7">
                  <a href="{SiteUrl}/terms.html" style="color:#FFFFFF;text-decoration:none">Terms &amp; Conditions</a> &nbsp;·&nbsp;
                  <a href="{SiteUrl}/terms.html" style="color:#FFFFFF;text-decoration:none">Privacy Policy</a> &nbsp;·&nbsp;
                  <a href="{SiteUrl}/contact.html" style="color:#FFFFFF;text-decoration:none">Contact us</a><br>
                  © {DateTime.UtcNow.Year} {AppName}. You're receiving this because you have an account with us.<br>
                  Powered by <a href="https://macdroidsoftwaresolutions.co.za/" style="color:#FFFFFF;text-decoration:none">Macdroid Software Solutions</a>
                </td></tr>
              </table>
            </td></tr>
          </table>
        </body>
        </html>
        """;

    private static string Greeting(string? name) => string.IsNullOrWhiteSpace(name) ? "Hello," : $"Hi {Enc(FirstName(name))},";

    private static string GreetingText(string? name) => string.IsNullOrWhiteSpace(name) ? "Hello," : $"Hi {FirstName(name)},";

    private static string FirstName(string name) => name.Trim().Split(' ', StringSplitOptions.RemoveEmptyEntries)[0];

    private static string Enc(string? value) => WebUtility.HtmlEncode(value ?? string.Empty);
}
