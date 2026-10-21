using DryCleaning.Api.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace DryCleaning.Api.Controllers;

/// <summary>Opens a verification document from a short-lived private link (made by <c>POST /api/me/documents/{typeId}/preview-link</c>).</summary>
[ApiController]
[Route("api/documents/preview")]
[AllowAnonymous]
public sealed class DocumentPreviewController(VerificationDocumentService documents) : ControllerBase
{
    /// <summary>The file, shown inline in the browser. The link is the credential, so it is signed, expires quickly and is never cached.</summary>
    [HttpGet("{token}")]
    public async Task<IActionResult> Get(string token, CancellationToken ct)
    {
        var (content, contentType, fileName) = await documents.OpenByPreviewLinkAsync(token, ct);
        Response.Headers.CacheControl = "no-store";
        Response.Headers["X-Content-Type-Options"] = "nosniff";
        Response.Headers["Referrer-Policy"] = "no-referrer";
        Response.Headers.ContentDisposition = $"inline; filename=\"{Uri.EscapeDataString(fileName)}\"";
        return File(content, contentType);
    }
}
