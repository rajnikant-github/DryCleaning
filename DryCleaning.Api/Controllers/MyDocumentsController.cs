using DryCleaning.Api.Common;
using DryCleaning.Api.Dtos;
using DryCleaning.Api.Infrastructure.Storage;
using DryCleaning.Api.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace DryCleaning.Api.Controllers;

/// <summary>A provider's or driver's own verification documents.</summary>
[ApiController]
[Route("api/me/documents")]
[Authorize(Roles = $"{Roles.Provider},{Roles.Driver}")]
[Produces("application/json")]
public sealed class MyDocumentsController(VerificationDocumentService documents) : ControllerBase
{
    public sealed class SubmitDocumentForm
    {
        /// <summary>A PDF, or a JPG, PNG, HEIC or WEBP image (PDF max 50 MB, photo max 15 MB by default; set with Verification:MaxPdfBytes / MaxImageBytes).</summary>
        public IFormFile File { get; set; } = null!;

        /// <summary>Required when the document type has a number (e.g. licence number).</summary>
        public string? Number { get; set; }

        /// <summary>Required when the document type has an expiry date.</summary>
        public DateTime? ExpiresOn { get; set; }
    }

    /// <summary>The documents the admin asks for, with my state for each (not submitted, pending, verified or rejected with a message).</summary>
    [HttpGet]
    public Task<MyDocumentsDto> Get(CancellationToken ct) => documents.GetMineAsync(ct);

    /// <summary>The file I uploaded for one document type, so I can check it. Only mine; never cached.</summary>
    [HttpGet("{typeId:guid}/file")]
    public async Task<IActionResult> File(Guid typeId, CancellationToken ct)
    {
        var (content, contentType, fileName) = await documents.OpenMineAsync(typeId, ct);
        Response.Headers.CacheControl = "no-store";
        Response.Headers["X-Content-Type-Options"] = "nosniff";
        Response.Headers.ContentDisposition = $"inline; filename=\"{Uri.EscapeDataString(fileName)}\"";
        return File(content, contentType);
    }

    /// <summary>
    /// A private link (works for a couple of minutes) that opens my uploaded file in a browser, e.g. a PDF preview.
    /// Tied to me and to this exact upload.
    /// </summary>
    [HttpPost("{typeId:guid}/preview-link")]
    public Task<PreviewLinkDto> PreviewLink(Guid typeId, CancellationToken ct) =>
        documents.CreatePreviewLinkAsync(typeId, $"{Request.Scheme}://{Request.Host}", ct);

    /// <summary>Upload (or replace) the document of one type. A new file always goes back to review.</summary>
    [HttpPost("{typeId:guid}")]
    [Consumes("multipart/form-data")]
    [ServiceFilter(typeof(VerificationUploadLimitFilter))]
    [RequestFormLimits(MultipartBodyLengthLimit = long.MaxValue)]
    public Task<MyDocumentsDto> Submit(Guid typeId, [FromForm] SubmitDocumentForm form, CancellationToken ct) =>
        documents.SubmitAsync(typeId, form.File, form.Number, form.ExpiresOn, ct);

}
