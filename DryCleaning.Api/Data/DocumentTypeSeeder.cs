using DryCleaning.Api.Domain;
using Microsoft.EntityFrameworkCore;

namespace DryCleaning.Api.Data;

/// <summary>
/// Creates the starting list of verification documents (South African marketplace) the first time the table is empty.
/// Admins then add, change or switch off types in the portal; the list is never re-created after that.
/// </summary>
public static class DocumentTypeSeeder
{
    public static async Task EnsureDefaultsAsync(AppDbContext db, CancellationToken ct)
    {
        if (await db.DocumentTypes.AnyAsync(ct))
            return;

        var order = 0;
        DocumentType Driver(string name, string? description, bool required, string? numberLabel = null, bool expiry = false) =>
            New(UserRole.Driver, name, description, required, numberLabel, expiry, ++order);
        DocumentType Provider(string name, string? description, bool required, string? numberLabel = null, bool expiry = false) =>
            New(UserRole.Provider, name, description, required, numberLabel, expiry, ++order);

        db.DocumentTypes.AddRange(
            Driver("ID document or passport", "A clear copy of both sides of your ID, or the photo page of your passport.", true, "ID / passport number"),
            Driver("Driver's licence", "Front and back of your valid driver's licence.", true, "Licence number", expiry: true),
            Driver("Selfie holding your ID", "A photo of your face holding your ID next to it, so we can match you to the document.", true),
            Driver("Proof of residence", "A utility bill or bank statement, not older than 3 months.", true),
            Driver("Vehicle licence disc / registration", "The licence disc or registration papers of the vehicle you will use.", true, "Registration number", expiry: true),
            Driver("Vehicle insurance", "Your motor or goods-in-transit insurance.", false, "Policy number", expiry: true),
            Driver("Police clearance certificate", "A SAPS police clearance certificate.", false, "Certificate number", expiry: true),
            Driver("Professional driving permit (PrDP)", "Only needed for some vehicle types.", false, "Permit number", expiry: true),
            Driver("Bank confirmation letter", "A letter from your bank confirming your account.", false, "Account number"));

        order = 0;
        db.DocumentTypes.AddRange(
            Provider("Owner's ID document or passport", "A clear copy of the business owner's ID, or the photo page of the passport.", true, "ID / passport number"),
            Provider("Business registration (CIPC certificate)", "Your company or business registration certificate.", true, "Registration number"),
            Provider("Proof of business address", "A utility bill, lease or bank statement showing the business address.", true),
            Provider("Tax clearance / SARS tax reference", "Your tax clearance certificate or SARS tax reference.", false, "Tax number", expiry: true),
            Provider("VAT registration certificate", "Only if the business is VAT-registered.", false, "VAT number"),
            Provider("Bank confirmation letter", "A letter from the business's bank confirming the account.", false, "Account number"),
            Provider("Business or trading licence / zoning approval", "If your municipality requires one.", false, "Licence number", expiry: true),
            Provider("Public liability insurance", "Insurance covering customers' items while in your care.", false, "Policy number", expiry: true),
            Provider("Photos of the premises", "A few photos of the shop or cleaning facility.", false));

        await db.SaveChangesAsync(ct);
    }

    private static DocumentType New(UserRole role, string name, string? description, bool required, string? numberLabel, bool expiry, int order) => new()
    {
        Role = role, Name = name, Description = description, IsRequired = required, NumberLabel = numberLabel, HasExpiry = expiry, DisplayOrder = order
    };
}
