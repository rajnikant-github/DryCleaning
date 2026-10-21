using DryCleaning.Api.Common;
using DryCleaning.Api.Domain;
using DryCleaning.Api.Dtos;
using DryCleaning.Api.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace DryCleaning.Api.Controllers;

/// <summary>Admin: which documents are required, and reviewing what providers and drivers uploaded.</summary>
[ApiController]
[Route("api/admin")]
[Authorize(Roles = Roles.Admin)]
[Produces("application/json")]
public sealed class AdminVerificationController(VerificationDocumentService documents) : ControllerBase
{
    // Document types
    /// <summary>All document types (on and off), optionally for one role.</summary>
    [HttpGet("document-types")]
    public Task<IReadOnlyList<DocumentTypeDto>> DocumentTypes([FromQuery] UserRole? role, CancellationToken ct) => documents.ListTypesAsync(role, ct);

    [HttpPost("document-types")]
    public Task<DocumentTypeDto> CreateDocumentType(DocumentTypeRequest request, CancellationToken ct) => documents.CreateTypeAsync(request, ct);

    [HttpPut("document-types/{id:guid}")]
    public Task<DocumentTypeDto> UpdateDocumentType(Guid id, DocumentTypeRequest request, CancellationToken ct) => documents.UpdateTypeAsync(id, request, ct);

    /// <summary>Switch a document type on or off without deleting it.</summary>
    [HttpPut("document-types/{id:guid}/active")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> SetDocumentTypeActive(Guid id, SetActiveRequest request, CancellationToken ct)
    {
        await documents.SetTypeActiveAsync(id, request.IsActive, ct);
        return NoContent();
    }

    /// <summary>Delete a type nobody has uploaded for yet (otherwise switch it off).</summary>
    [HttpDelete("document-types/{id:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> DeleteDocumentType(Guid id, CancellationToken ct)
    {
        await documents.DeleteTypeAsync(id, ct);
        return NoContent();
    }

    // Review
    /// <summary>Applicants with documents waiting for review (or, with <c>onlyPending=false</c>, everyone waiting for approval).</summary>
    [HttpGet("verification")]
    public Task<IReadOnlyList<VerificationQueueItemDto>> Queue([FromQuery] bool onlyPending = true, [FromQuery] UserRole? role = null, CancellationToken ct = default) =>
        documents.GetQueueAsync(onlyPending, role, ct);

    /// <summary>One applicant with every requested document, its state and its history.</summary>
    [HttpGet("verification/{userId:guid}")]
    public Task<VerificationApplicantDto> Applicant(Guid userId, CancellationToken ct) => documents.GetApplicantAsync(userId, ct);

    /// <summary>The uploaded file itself (recorded in the document's history). Never cached.</summary>
    [HttpGet("verification/documents/{documentId:guid}/file")]
    public async Task<IActionResult> File(Guid documentId, CancellationToken ct)
    {
        var (content, contentType, fileName) = await documents.OpenForAdminAsync(documentId, ct);
        Response.Headers.CacheControl = "no-store";
        Response.Headers["X-Content-Type-Options"] = "nosniff";
        Response.Headers.ContentDisposition = $"inline; filename=\"{Uri.EscapeDataString(fileName)}\"";
        return File(content, contentType);
    }

    [HttpPost("verification/documents/{documentId:guid}/verify")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> Verify(Guid documentId, CancellationToken ct)
    {
        await documents.VerifyAsync(documentId, ct);
        return NoContent();
    }

    /// <summary>Reject with a message the applicant will see.</summary>
    [HttpPost("verification/documents/{documentId:guid}/reject")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> Reject(Guid documentId, RejectDocumentRequest request, CancellationToken ct)
    {
        await documents.RejectAsync(documentId, request.Message, ct);
        return NoContent();
    }
}
