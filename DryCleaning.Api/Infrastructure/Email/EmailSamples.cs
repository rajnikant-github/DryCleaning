using DryCleaning.Api.Domain;

namespace DryCleaning.Api.Infrastructure.Email;

/// <summary>One sample of every email the API sends, filled with made-up data, so admins can check how each one looks in a real inbox.</summary>
public static class EmailSamples
{
    public static IReadOnlyList<EmailMessage> All(string to)
    {
        var messages = new List<EmailMessage>
        {
            EmailTemplates.Test(to),
            EmailTemplates.Welcome(to, "Alex", UserRole.Customer),
            EmailTemplates.Welcome(to, "Alex", UserRole.Provider),
            EmailTemplates.Welcome(to, "Alex", UserRole.Driver),
            EmailTemplates.Code(to, "Alex", "482913", 10, passwordReset: false),
            EmailTemplates.Code(to, "Alex", "731095", 10, passwordReset: true),
            EmailTemplates.OrderConfirmation(to, "Alex", SampleOrder(), "Sparkle Dry Cleaners", "ZAR"),
            EmailTemplates.Announcement(to, "Alex", "Scheduled maintenance tonight", "The app will be unavailable between 23:00 and 23:30 while we make improvements.")
        };

        // Make it obvious these are samples (and keep them apart in the inbox).
        return messages.Select(m => new EmailMessage { To = m.To, Subject = $"[Sample] {m.Subject}", Html = m.Html, Text = m.Text }).ToList();
    }

    private static Order SampleOrder()
    {
        var order = new Order { OrderNumber = "ED-SAMPLE", PickupDate = DateTime.Today.AddDays(1), PickupSlot = "09:00 AM – 11:00 AM" };
        order.Items.Add(new OrderItem { Quantity = 2, UnitPrice = 80m, TotalPrice = 160m, Product = new Product { Name = "Suit Cleaning" }, ServiceItem = new ServiceItem { Name = "Jacket" } });
        order.Items.Add(new OrderItem { Quantity = 3, UnitPrice = 30m, TotalPrice = 90m, Product = new Product { Name = "Shirts" }, ServiceItem = new ServiceItem { Name = "Wash & iron" } });

        // The same calculator real orders use: a 5% service fee (with VAT), a fixed fee, 15% VAT.
        var charges = new[]
        {
            new ExtraCharge { Name = "Service fee", Type = ExtraChargeType.Percentage, Percent = 5m, IsTaxable = true, IsActive = true, DisplayOrder = 1 },
            new ExtraCharge { Name = "Eco levy", Type = ExtraChargeType.Fixed, Amount = 10m, IsActive = true, DisplayOrder = 2 }
        };
        order.Reprice(45m, charges, new VatSetting { Rate = 15m, IsEnabled = true });
        return order;
    }
}
