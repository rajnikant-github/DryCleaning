using System.Text.RegularExpressions;
using DryCleaning.Api.Common;
using DryCleaning.Api.Data;
using DryCleaning.Api.Domain;
using DryCleaning.Api.Dtos;
using DryCleaning.Api.Infrastructure.Email;
using DryCleaning.Api.Infrastructure.Storage;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace DryCleaning.Api.Services;

/// <summary>
/// Identity and business documents that providers and drivers upload and admins verify. Admins decide which documents are
/// asked for (<see cref="DocumentType"/>); the account can only be approved once every required one is verified.
/// Files live in protected storage and are only ever streamed after a permission check.
/// </summary>
public sealed class VerificationDocumentService(AppDbContext db, ICurrentUser currentUser, IFileStorage storage, EmailQueue email,
    IOptions<VerificationOptions> options, IDataProtectionProvider dataProtection, ILogger<VerificationDocumentService> logger)
{
    private static readonly string[] AllowedExtensions = [".pdf", ".jpg", ".jpeg", ".png", ".heic", ".webp"];

    // ── The applicant (provider or driver) ───────────────────────────────────

    public async Task<MyDocumentsDto> GetMineAsync(CancellationToken ct)
    {
        var (userId, role) = Applicant();
        var types = await ActiveTypesAsync(role, ct);
        var docs = await db.VerificationDocuments.AsNoTracking().Where(d => d.UserId == userId).ToListAsync(ct);

        var items = types.Select(t =>
        {
            var doc = docs.FirstOrDefault(d => d.DocumentTypeId == t.Id);
            return new MyDocumentDto
            {
                TypeId = t.Id, Name = t.Name, Description = t.Description, IsRequired = t.IsRequired,
                NumberLabel = t.NumberLabel, HasExpiry = t.HasExpiry,
                State = StateOf(doc), Number = doc?.Number, ExpiresOn = doc?.ExpiresOn, FileName = doc?.FileName,
                RejectionMessage = doc?.Status == DocumentStatus.Rejected ? doc.RejectionMessage : null,
                SubmittedAt = doc?.SubmittedAt
            };
        }).ToList();

        var required = items.Where(i => i.IsRequired).ToList();
        return new MyDocumentsDto
        {
            Items = items,
            RequiredCount = required.Count,
            VerifiedRequiredCount = required.Count(i => i.State == DocumentState.Verified),
            ActionNeededCount = required.Count(i => i.State is DocumentState.NotSubmitted or DocumentState.Rejected),
            PendingRequiredCount = required.Count(i => i.State == DocumentState.Pending),
            AllRequiredVerified = required.All(i => i.State == DocumentState.Verified),
            MaxPdfBytes = options.Value.MaxPdfBytes,
            MaxImageBytes = options.Value.MaxImageBytes
        };
    }

    public async Task<MyDocumentsDto> SubmitAsync(Guid typeId, IFormFile file, string? number, DateTime? expiresOn, CancellationToken ct)
    {
        var (userId, role) = Applicant();
        var type = await db.DocumentTypes.FirstOrDefaultAsync(t => t.Id == typeId && t.IsActive && t.Role == role, ct)
                   ?? throw new NotFoundException("That document isn't requested.");

        number = CleanNumber(type, number);
        var expiry = CleanExpiry(type, expiresOn);
        var extension = await ValidateFileAsync(file, ct);

        string stored;
        await using (var stream = file.OpenReadStream())
            stored = await storage.SaveAsync(stream, extension, $"verification/{userId:N}", FileVisibility.Protected, ct);

        var doc = await db.VerificationDocuments.FirstOrDefaultAsync(d => d.UserId == userId && d.DocumentTypeId == typeId, ct);
        var oldPath = doc?.FilePath;
        if (doc is null)
        {
            doc = new VerificationDocument { UserId = userId, DocumentTypeId = typeId };
            db.VerificationDocuments.Add(doc);
        }

        doc.FilePath = stored;
        doc.FileName = SafeFileName(file.FileName);
        doc.ContentType = ContentTypeFor(extension);
        doc.SizeBytes = file.Length;
        doc.Number = number;
        doc.ExpiresOn = expiry;
        doc.Status = DocumentStatus.Pending; // a new file always goes back to review
        doc.RejectionMessage = null;
        doc.ReviewedByUserId = null;
        doc.ReviewedAt = null;
        doc.SubmittedAt = DateTime.UtcNow;
        doc.Touch();
        db.DocumentReviewEvents.Add(new DocumentReviewEvent { VerificationDocumentId = doc.Id, Action = DocumentAction.Submitted, ActorUserId = userId });
        await db.SaveChangesAsync(ct);

        await DeleteQuietlyAsync(oldPath, ct);
        await NotifyAdminsWhenCompleteAsync(userId, role, ct);
        return await GetMineAsync(ct);
    }

    /// <summary>Opens the applicant's own uploaded file so they can check what they sent. Only the owner (never another user).</summary>
    public async Task<(Stream Content, string ContentType, string FileName)> OpenMineAsync(Guid typeId, CancellationToken ct)
    {
        var (userId, _) = Applicant();
        var doc = await db.VerificationDocuments.AsNoTracking().FirstOrDefaultAsync(d => d.UserId == userId && d.DocumentTypeId == typeId, ct)
                  ?? throw new NotFoundException("You haven't uploaded this document yet.");
        try
        {
            var (content, _) = await storage.OpenReadAsync(doc.FilePath, ct);
            return (content, doc.ContentType, doc.FileName);
        }
        catch (FileNotFoundException)
        {
            throw new NotFoundException("The file is no longer available. Please upload it again.");
        }
    }

    /// <summary>
    /// A short-lived link that opens the applicant's own upload in a browser (a browser can't send the sign-in token). The link is signed and
    /// tied to this user and this exact upload, so it stops working when the file is replaced, and it expires after
    /// <see cref="VerificationOptions.PreviewLinkSeconds"/>.
    /// </summary>
    public async Task<PreviewLinkDto> CreatePreviewLinkAsync(Guid typeId, string baseUrl, CancellationToken ct)
    {
        var (userId, _) = Applicant();
        var doc = await db.VerificationDocuments.AsNoTracking().FirstOrDefaultAsync(d => d.UserId == userId && d.DocumentTypeId == typeId, ct)
                  ?? throw new NotFoundException("You haven't uploaded this document yet.");

        var seconds = Math.Max(30, options.Value.PreviewLinkSeconds);
        var token = PreviewProtector.Protect($"{userId:N}|{doc.Id:N}|{doc.SubmittedAt.Ticks}", TimeSpan.FromSeconds(seconds));
        return new PreviewLinkDto { Url = $"{baseUrl.TrimEnd('/')}/api/documents/preview/{token}", ExpiresInSeconds = seconds };
    }

    /// <summary>Opens a file from a preview link made by <see cref="CreatePreviewLinkAsync"/>. Anyone holding a valid, unexpired link can open it.</summary>
    public async Task<(Stream Content, string ContentType, string FileName)> OpenByPreviewLinkAsync(string token, CancellationToken ct)
    {
        const string expired = "This link has expired. Open the document again from the app.";
        string[] parts;
        try
        {
            parts = PreviewProtector.Unprotect(token).Split('|');
        }
        catch (System.Security.Cryptography.CryptographicException)
        {
            throw new NotFoundException(expired);
        }

        if (parts.Length != 3 || !Guid.TryParseExact(parts[0], "N", out var userId) || !Guid.TryParseExact(parts[1], "N", out var docId) || !long.TryParse(parts[2], out var ticks))
            throw new NotFoundException(expired);

        var doc = await db.VerificationDocuments.AsNoTracking().FirstOrDefaultAsync(d => d.Id == docId && d.UserId == userId, ct);
        if (doc is null || doc.SubmittedAt.Ticks != ticks)
            throw new NotFoundException(expired); // replaced or deleted since the link was made

        try
        {
            var (content, _) = await storage.OpenReadAsync(doc.FilePath, ct);
            return (content, doc.ContentType, doc.FileName);
        }
        catch (FileNotFoundException)
        {
            throw new NotFoundException(expired);
        }
    }

    private Microsoft.AspNetCore.DataProtection.ITimeLimitedDataProtector PreviewProtector =>
        dataProtection.CreateProtector("verification-document-preview").ToTimeLimitedDataProtector();

    // ── Approval gate (used by the provider / driver approval) ───────────────

    /// <summary>Throws a 409 explaining what is missing, waiting or rejected until every required document is verified.</summary>
    public async Task EnsureRequiredVerifiedAsync(Guid userId, UserRole role, CancellationToken ct)
    {
        var required = (await ActiveTypesAsync(role, ct)).Where(t => t.IsRequired).ToList();
        if (required.Count == 0)
            return;

        var docs = await db.VerificationDocuments.AsNoTracking().Where(d => d.UserId == userId).ToListAsync(ct);
        var missing = new List<string>();
        var pending = new List<string>();
        var rejected = new List<string>();
        foreach (var type in required)
        {
            var doc = docs.FirstOrDefault(d => d.DocumentTypeId == type.Id);
            switch (StateOf(doc))
            {
                case DocumentState.NotSubmitted: missing.Add(type.Name); break;
                case DocumentState.Pending: pending.Add(type.Name); break;
                case DocumentState.Rejected: rejected.Add(type.Name); break;
            }
        }

        if (missing.Count + pending.Count + rejected.Count == 0)
            return;

        var parts = new List<string>();
        if (missing.Count > 0) parts.Add($"not uploaded: {string.Join(", ", missing)}");
        if (pending.Count > 0) parts.Add($"waiting for your review: {string.Join(", ", pending)}");
        if (rejected.Count > 0) parts.Add($"rejected: {string.Join(", ", rejected)}");
        throw new ConflictException($"Can't approve yet – every required document must be verified first ({string.Join("; ", parts)}).");
    }

    // ── Admin: document types ────────────────────────────────────────────────

    public async Task<IReadOnlyList<DocumentTypeDto>> ListTypesAsync(UserRole? role, CancellationToken ct) =>
        (await db.DocumentTypes.AsNoTracking().Where(t => role == null || t.Role == role)
            .OrderBy(t => t.Role).ThenBy(t => t.DisplayOrder).ThenBy(t => t.Name).ToListAsync(ct)).Select(ToDto).ToList();

    public async Task<DocumentTypeDto> CreateTypeAsync(DocumentTypeRequest request, CancellationToken ct)
    {
        await EnsureNameFreeAsync(request, null, ct);
        var type = new DocumentType();
        Apply(type, request);
        db.DocumentTypes.Add(type);
        await db.SaveChangesAsync(ct);
        return ToDto(type);
    }

    public async Task<DocumentTypeDto> UpdateTypeAsync(Guid id, DocumentTypeRequest request, CancellationToken ct)
    {
        var type = await db.DocumentTypes.FirstOrDefaultAsync(t => t.Id == id, ct) ?? throw new NotFoundException("Document type not found.");
        if (type.Role != request.Role && await db.VerificationDocuments.AnyAsync(d => d.DocumentTypeId == id, ct))
            throw new ConflictException("Documents were already uploaded for this type, so its role can't change.");

        await EnsureNameFreeAsync(request, id, ct);
        Apply(type, request);
        type.Touch();
        await db.SaveChangesAsync(ct);
        return ToDto(type);
    }

    public async Task SetTypeActiveAsync(Guid id, bool isActive, CancellationToken ct)
    {
        var type = await db.DocumentTypes.FirstOrDefaultAsync(t => t.Id == id, ct) ?? throw new NotFoundException("Document type not found.");
        type.IsActive = isActive;
        type.Touch();
        await db.SaveChangesAsync(ct);
    }

    public async Task DeleteTypeAsync(Guid id, CancellationToken ct)
    {
        var type = await db.DocumentTypes.FirstOrDefaultAsync(t => t.Id == id, ct) ?? throw new NotFoundException("Document type not found.");
        if (await db.VerificationDocuments.AnyAsync(d => d.DocumentTypeId == id, ct))
            throw new ConflictException("Documents were already uploaded for this type. Switch it off instead of deleting it.");
        db.DocumentTypes.Remove(type);
        await db.SaveChangesAsync(ct);
    }

    // ── Admin: review ────────────────────────────────────────────────────────

    /// <summary>Applicants with something to review (or, with <paramref name="onlyPending"/> off, everyone still waiting for approval).</summary>
    public async Task<IReadOnlyList<VerificationQueueItemDto>> GetQueueAsync(bool onlyPending, UserRole? role, CancellationToken ct)
    {
        var docs = await db.VerificationDocuments.AsNoTracking().Include(d => d.DocumentType).Where(d => d.DocumentType.IsActive).ToListAsync(ct);
        var providers = await db.ServiceProviders.AsNoTracking().ToListAsync(ct);
        var drivers = await db.Drivers.AsNoTracking().ToListAsync(ct);
        var types = await db.DocumentTypes.AsNoTracking().Where(t => t.IsActive && t.IsRequired).ToListAsync(ct);

        var applicants = providers.Select(p => (UserId: p.UserId, Role: UserRole.Provider, Detail: (string?)p.BusinessName, Status: ProfileStatusExtensions.StatusOf(p.IsActive, p.ApprovedAt)))
            .Concat(drivers.Select(d => (UserId: d.UserId, Role: UserRole.Driver, Detail: (string?)d.LicenseNumber, Status: ProfileStatusExtensions.StatusOf(d.IsActive, d.ApprovedAt))))
            .Where(a => role == null || a.Role == role)
            .ToList();

        var users = await db.Users.AsNoTracking().Where(u => applicants.Select(a => a.UserId).Contains(u.Id)).ToDictionaryAsync(u => u.Id, ct);

        var items = new List<VerificationQueueItemDto>();
        foreach (var a in applicants)
        {
            var mine = docs.Where(d => d.UserId == a.UserId).ToList();
            var pending = mine.Count(d => d.Status == DocumentStatus.Pending);
            var waitingForApproval = a.Status == ProfileStatus.PendingApproval;
            if (onlyPending ? pending == 0 : !(waitingForApproval || mine.Count > 0))
                continue;

            var requiredTypes = types.Where(t => t.Role == a.Role).ToList();
            var verifiedRequired = requiredTypes.Count(t => mine.Any(d => d.DocumentTypeId == t.Id && d.Status == DocumentStatus.Verified));
            var user = users[a.UserId];
            items.Add(new VerificationQueueItemDto
            {
                UserId = a.UserId, Name = user.Name, Email = user.Email, Role = a.Role, Detail = a.Detail, ProfileStatus = a.Status,
                PendingCount = pending,
                VerifiedCount = mine.Count(d => d.Status == DocumentStatus.Verified),
                RejectedCount = mine.Count(d => d.Status == DocumentStatus.Rejected),
                RequiredRemaining = requiredTypes.Count - verifiedRequired,
                LastSubmittedAt = mine.Count == 0 ? null : mine.Max(d => d.SubmittedAt)
            });
        }

        return items.OrderByDescending(i => i.PendingCount > 0).ThenBy(i => i.LastSubmittedAt ?? DateTime.MaxValue).ToList();
    }

    public async Task<VerificationApplicantDto> GetApplicantAsync(Guid userId, CancellationToken ct)
    {
        var user = await db.Users.AsNoTracking().FirstOrDefaultAsync(u => u.Id == userId && (u.Role == UserRole.Provider || u.Role == UserRole.Driver), ct)
                   ?? throw new NotFoundException("Applicant not found.");

        Guid? profileId;
        string? business = null, licence = null;
        ProfileStatus status;
        if (user.Role == UserRole.Provider)
        {
            var provider = await db.ServiceProviders.AsNoTracking().FirstAsync(p => p.UserId == userId, ct);
            (profileId, business, status) = (provider.Id, provider.BusinessName, ProfileStatusExtensions.StatusOf(provider.IsActive, provider.ApprovedAt));
        }
        else
        {
            var driver = await db.Drivers.AsNoTracking().FirstAsync(d => d.UserId == userId, ct);
            (profileId, licence, status) = (driver.Id, driver.LicenseNumber, ProfileStatusExtensions.StatusOf(driver.IsActive, driver.ApprovedAt));
        }

        var docs = await db.VerificationDocuments.AsNoTracking().Include(d => d.Events).Where(d => d.UserId == userId).ToListAsync(ct);
        var activeTypes = await ActiveTypesAsync(user.Role, ct);
        var extraTypeIds = docs.Select(d => d.DocumentTypeId).Except(activeTypes.Select(t => t.Id)).ToList();
        var types = activeTypes.Concat(await db.DocumentTypes.AsNoTracking().Where(t => extraTypeIds.Contains(t.Id)).ToListAsync(ct)).ToList();

        var actorIds = docs.SelectMany(d => d.Events).Select(e => e.ActorUserId).Distinct().ToList();
        var actors = await db.Users.AsNoTracking().Where(u => actorIds.Contains(u.Id)).ToDictionaryAsync(u => u.Id, u => u.Name, ct);

        var rows = types.Select(t =>
        {
            var doc = docs.FirstOrDefault(d => d.DocumentTypeId == t.Id);
            return new ReviewDocumentDto
            {
                DocumentId = doc?.Id, TypeId = t.Id, Name = t.Name, IsRequired = t.IsRequired, TypeActive = t.IsActive, NumberLabel = t.NumberLabel, HasExpiry = t.HasExpiry,
                State = StateOf(doc), Number = doc?.Number, ExpiresOn = doc?.ExpiresOn, FileName = doc?.FileName, ContentType = doc?.ContentType,
                SizeBytes = doc?.SizeBytes, SubmittedAt = doc?.SubmittedAt, ReviewedAt = doc?.ReviewedAt,
                RejectionMessage = doc?.Status == DocumentStatus.Rejected ? doc.RejectionMessage : null,
                History = (doc?.Events ?? []).OrderBy(e => e.CreatedAt).Select(e => new DocumentEventDto
                {
                    Action = e.Action, Message = e.Message, CreatedAt = e.CreatedAt, ActorName = actors.GetValueOrDefault(e.ActorUserId, "Unknown")
                }).ToList()
            };
        }).ToList();

        var required = rows.Where(r => r.IsRequired && r.TypeActive).ToList();
        return new VerificationApplicantDto
        {
            UserId = user.Id, ProfileId = profileId, Name = user.Name, Email = user.Email, Phone = user.Phone, Role = user.Role,
            BusinessName = business, LicenseNumber = licence, ProfileStatus = status, Documents = rows,
            RequiredCount = required.Count,
            VerifiedRequiredCount = required.Count(r => r.State == DocumentState.Verified),
            CanApprove = required.All(r => r.State == DocumentState.Verified)
        };
    }

    /// <summary>Opens the uploaded file for an admin (recorded in the document's history).</summary>
    public async Task<(Stream Content, string ContentType, string FileName)> OpenForAdminAsync(Guid documentId, CancellationToken ct)
    {
        var doc = await db.VerificationDocuments.FirstOrDefaultAsync(d => d.Id == documentId, ct) ?? throw new NotFoundException("Document not found.");
        try
        {
            var (content, _) = await storage.OpenReadAsync(doc.FilePath, ct);
            db.DocumentReviewEvents.Add(new DocumentReviewEvent { VerificationDocumentId = doc.Id, Action = DocumentAction.Viewed, ActorUserId = AdminId() });
            await db.SaveChangesAsync(ct);
            return (content, doc.ContentType, doc.FileName);
        }
        catch (FileNotFoundException)
        {
            throw new NotFoundException("The file is no longer available.");
        }
    }

    public async Task VerifyAsync(Guid documentId, CancellationToken ct)
    {
        var doc = await LoadForReviewAsync(documentId, ct);
        doc.Status = DocumentStatus.Verified;
        doc.RejectionMessage = null;
        Stamp(doc, DocumentAction.Verified, null);

        db.Notify(doc.UserId, null, "Document verified", $"Your {doc.DocumentType.Name} was verified.", NotificationType.System);
        await db.SaveChangesAsync(ct);

        // Everything required is now verified: tell them only the final approval is left.
        if ((await ActiveTypesAsync(doc.User.Role, ct)).Where(t => t.IsRequired).All(t => db.VerificationDocuments.Any(d => d.UserId == doc.UserId && d.DocumentTypeId == t.Id && d.Status == DocumentStatus.Verified)))
        {
            db.Notify(doc.UserId, null, "Documents verified", "All your required documents are verified. An admin will approve your account next.", NotificationType.System);
            await db.SaveChangesAsync(ct);
        }
    }

    public async Task RejectAsync(Guid documentId, string message, CancellationToken ct)
    {
        var doc = await LoadForReviewAsync(documentId, ct);
        message = message.Trim();
        doc.Status = DocumentStatus.Rejected;
        doc.RejectionMessage = message;
        Stamp(doc, DocumentAction.Rejected, message);

        db.Notify(doc.UserId, null, "Document rejected", $"Your {doc.DocumentType.Name} was rejected: {message}", NotificationType.System);
        await db.SaveChangesAsync(ct);

        if (!string.IsNullOrWhiteSpace(doc.User.Email))
            email.Enqueue(EmailTemplates.DocumentRejected(doc.User.Email, doc.User.Name, doc.DocumentType.Name, message));
    }

    // ── Retention ────────────────────────────────────────────────────────────

    /// <summary>Deletes documents whose application was rejected, or whose account was deactivated, longer ago than the retention period.</summary>
    public async Task<int> PurgeAsync(CancellationToken ct)
    {
        var months = options.Value.RetentionMonths;
        if (months <= 0)
            return 0;

        var cutoff = DateTime.UtcNow.AddMonths(-months);
        var deactivated = await db.ServiceProviders.Where(p => !p.IsActive && p.ApprovedAt != null && p.UpdatedAt < cutoff).Select(p => p.UserId)
            .Concat(db.Drivers.Where(d => !d.IsActive && d.ApprovedAt != null && d.UpdatedAt < cutoff).Select(d => d.UserId))
            .ToListAsync(ct);

        var old = await db.VerificationDocuments
            .Where(d => (d.Status == DocumentStatus.Rejected && d.ReviewedAt != null && d.ReviewedAt < cutoff) || deactivated.Contains(d.UserId))
            .ToListAsync(ct);
        if (old.Count == 0)
            return 0;

        // The database never cascades deletes, so remove each document's history first; files go last, so a failed save never leaves a row pointing at a missing file.
        var ids = old.Select(d => d.Id).ToList();
        db.DocumentReviewEvents.RemoveRange(await db.DocumentReviewEvents.Where(e => ids.Contains(e.VerificationDocumentId)).ToListAsync(ct));
        db.VerificationDocuments.RemoveRange(old);
        await db.SaveChangesAsync(ct);
        foreach (var doc in old)
            await DeleteQuietlyAsync(doc.FilePath, ct);
        logger.LogInformation("Deleted {Count} verification documents older than {Months} months", old.Count, months);
        return old.Count;
    }

    // ── Helpers ──────────────────────────────────────────────────────────────

    private (Guid UserId, UserRole Role) Applicant()
    {
        var userId = currentUser.UserId ?? throw new UnauthorizedException("You need to sign in.");
        if (currentUser.Role is not (UserRole.Provider or UserRole.Driver))
            throw new ForbiddenException("Only service providers and drivers upload verification documents.");
        return (userId, currentUser.Role.Value);
    }

    private Guid AdminId() => currentUser.UserId ?? throw new UnauthorizedException("You need to sign in.");

    private Task<List<DocumentType>> ActiveTypesAsync(UserRole role, CancellationToken ct) =>
        db.DocumentTypes.AsNoTracking().Where(t => t.Role == role && t.IsActive).OrderBy(t => t.DisplayOrder).ThenBy(t => t.Name).ToListAsync(ct);

    private async Task<VerificationDocument> LoadForReviewAsync(Guid documentId, CancellationToken ct) =>
        await db.VerificationDocuments.Include(d => d.DocumentType).Include(d => d.User).FirstOrDefaultAsync(d => d.Id == documentId, ct)
        ?? throw new NotFoundException("Document not found.");

    private void Stamp(VerificationDocument doc, DocumentAction action, string? message)
    {
        var admin = AdminId();
        doc.ReviewedByUserId = admin;
        doc.ReviewedAt = DateTime.UtcNow;
        doc.Touch();
        db.DocumentReviewEvents.Add(new DocumentReviewEvent { VerificationDocumentId = doc.Id, Action = action, ActorUserId = admin, Message = message });
    }

    private static DocumentState StateOf(VerificationDocument? doc) => doc?.Status switch
    {
        null => DocumentState.NotSubmitted,
        DocumentStatus.Verified => DocumentState.Verified,
        DocumentStatus.Rejected => DocumentState.Rejected,
        _ => DocumentState.Pending
    };

    /// <summary>When the applicant has now uploaded everything required (nothing missing or rejected), tell the admins there is something to review.</summary>
    private async Task NotifyAdminsWhenCompleteAsync(Guid userId, UserRole role, CancellationToken ct)
    {
        var required = (await ActiveTypesAsync(role, ct)).Where(t => t.IsRequired).Select(t => t.Id).ToList();
        var docs = await db.VerificationDocuments.AsNoTracking().Where(d => d.UserId == userId).ToListAsync(ct);
        var allSubmitted = required.All(id => docs.Any(d => d.DocumentTypeId == id && d.Status != DocumentStatus.Rejected));
        if (!allSubmitted || !docs.Any(d => d.Status == DocumentStatus.Pending))
            return;

        var name = await db.Users.Where(u => u.Id == userId).Select(u => u.Name).FirstAsync(ct);
        foreach (var adminId in await db.Users.Where(u => u.Role == UserRole.Admin && u.IsActive).Select(u => u.Id).ToListAsync(ct))
            db.Notify(adminId, null, "Documents ready for review", $"{name} ({role}) has uploaded documents to verify.", NotificationType.System);
        await db.SaveChangesAsync(ct);
    }

    private static string? CleanNumber(DocumentType type, string? number)
    {
        if (string.IsNullOrWhiteSpace(type.NumberLabel))
            return null;

        number = number?.Trim();
        if (string.IsNullOrEmpty(number))
            throw new BadRequestException($"Enter the {Phrase(type.NumberLabel)}.");
        if (number.Length > 60)
            throw new BadRequestException($"The {Phrase(type.NumberLabel)} is too long.");
        if (!string.IsNullOrWhiteSpace(type.NumberPattern) && !Regex.IsMatch(number, type.NumberPattern, RegexOptions.None, TimeSpan.FromSeconds(1)))
            throw new BadRequestException($"That doesn't look like a valid {Phrase(type.NumberLabel)}.");
        return number;
    }

    /// <summary>Lower-cases the first letter of a label in a sentence, but keeps acronyms such as "ID / passport number".</summary>
    private static string Phrase(string label) => label.Length > 1 && char.IsLower(label[1]) ? char.ToLowerInvariant(label[0]) + label[1..] : label;

    private static DateTime? CleanExpiry(DocumentType type, DateTime? expiresOn)
    {
        if (!type.HasExpiry)
            return null;
        if (expiresOn is null)
            throw new BadRequestException("Enter the expiry date.");
        if (expiresOn.Value.Date < DateTime.UtcNow.Date)
            throw new BadRequestException("That document has already expired. Upload a current one.");
        return expiresOn.Value.Date;
    }

    /// <summary>Checks size, extension and that the file really is what its extension says (not just renamed).</summary>
    private async Task<string> ValidateFileAsync(IFormFile file, CancellationToken ct)
    {
        if (file.Length == 0)
            throw new BadRequestException("The file is empty.");

        var extension = Path.GetExtension(file.FileName).ToLowerInvariant();
        if (!AllowedExtensions.Contains(extension))
            throw new BadRequestException("Upload a PDF, or a JPG, PNG, HEIC or WEBP image.");

        // PDFs can be big scans; photos are expected to arrive already shrunk by the app, so their limit is lower.
        var isPdf = extension == ".pdf";
        var limit = isPdf ? options.Value.MaxPdfBytes : options.Value.MaxImageBytes;
        if (file.Length > limit)
            throw new BadRequestException(isPdf
                ? $"PDF files must be smaller than {MaxSizeText(limit)}."
                : $"Photos must be smaller than {MaxSizeText(limit)}. Take the photo again or choose a smaller one.");

        var head = new byte[12];
        await using (var stream = file.OpenReadStream())
            _ = await stream.ReadAtLeastAsync(head, head.Length, throwOnEndOfStream: false, ct);

        var looksRight = extension switch
        {
            ".pdf" => head.AsSpan(0, 5).SequenceEqual("%PDF-"u8),
            ".jpg" or ".jpeg" => head[0] == 0xFF && head[1] == 0xD8 && head[2] == 0xFF,
            ".png" => head.AsSpan(0, 8).SequenceEqual(new byte[] { 0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A }),
            ".webp" => head.AsSpan(0, 4).SequenceEqual("RIFF"u8) && head.AsSpan(8, 4).SequenceEqual("WEBP"u8),
            ".heic" => head.AsSpan(4, 4).SequenceEqual("ftyp"u8),
            _ => false
        };
        if (!looksRight)
            throw new BadRequestException("That file doesn't look like a valid " + extension.TrimStart('.').ToUpperInvariant() + ".");
        return extension;
    }

    private static string MaxSizeText(long bytes) => bytes % (1024 * 1024) == 0 ? $"{bytes / (1024 * 1024)} MB" : bytes >= 1024 * 1024 ? $"{bytes / (1024.0 * 1024.0):0.#} MB" : $"{bytes / 1024} KB";

    private static string ContentTypeFor(string extension) => extension switch
    {
        ".pdf" => "application/pdf",
        ".png" => "image/png",
        ".webp" => "image/webp",
        ".heic" => "image/heic",
        _ => "image/jpeg"
    };

    private static string SafeFileName(string name)
    {
        var clean = new string(Path.GetFileName(name).Where(c => !char.IsControl(c)).ToArray()).Trim();
        return clean.Length == 0 ? "document" : clean.Length > 200 ? clean[^200..] : clean;
    }

    private async Task DeleteQuietlyAsync(string? path, CancellationToken ct)
    {
        if (string.IsNullOrEmpty(path))
            return;
        try
        {
            await storage.DeleteAsync(path, ct);
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Couldn't delete a replaced verification document");
        }
    }

    private async Task EnsureNameFreeAsync(DocumentTypeRequest request, Guid? exceptId, CancellationToken ct)
    {
        var name = request.Name.Trim();
        if (await db.DocumentTypes.AnyAsync(t => t.Role == request.Role && t.Name == name && t.Id != exceptId, ct))
            throw new ConflictException($"There is already a \"{name}\" document for {request.Role.ToString().ToLowerInvariant()}s.");
    }

    private static void Apply(DocumentType type, DocumentTypeRequest request)
    {
        type.Role = request.Role;
        type.Name = request.Name.Trim();
        type.Description = string.IsNullOrWhiteSpace(request.Description) ? null : request.Description.Trim();
        type.IsRequired = request.IsRequired;
        type.NumberLabel = string.IsNullOrWhiteSpace(request.NumberLabel) ? null : request.NumberLabel.Trim();
        type.NumberPattern = type.NumberLabel is null || string.IsNullOrWhiteSpace(request.NumberPattern) ? null : request.NumberPattern.Trim();
        type.HasExpiry = request.HasExpiry;
        type.IsActive = request.IsActive;
        type.DisplayOrder = request.DisplayOrder;
    }

    private static DocumentTypeDto ToDto(DocumentType t) => new()
    {
        Id = t.Id, Role = t.Role, Name = t.Name, Description = t.Description, IsRequired = t.IsRequired, NumberLabel = t.NumberLabel,
        NumberPattern = t.NumberPattern, HasExpiry = t.HasExpiry, IsActive = t.IsActive, DisplayOrder = t.DisplayOrder
    };
}
