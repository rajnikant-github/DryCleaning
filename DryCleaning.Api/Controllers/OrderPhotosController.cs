using DryCleaning.Api.Common;
using DryCleaning.Api.Dtos;
using DryCleaning.Api.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace DryCleaning.Api.Controllers;

/// <summary>Photo verification for an order (pickup, inspection, completion, damage).</summary>
[ApiController]
[Route("api/orders/{orderId:guid}/photos")]
[Authorize]
[Produces("application/json")]
public sealed class OrderPhotosController(OrderPhotoService photos) : ControllerBase
{
    public sealed class UploadPhotoForm
    {
        /// <summary>JPG, PNG, HEIC or WEBP (max 5 MB by default).</summary>
        public IFormFile File { get; set; } = null!;

        public Domain.PhotoType Type { get; set; }
    }

    [HttpPost]
    [Consumes("multipart/form-data")]
    [RequestSizeLimit(10 * 1024 * 1024)]
    public Task<OrderPhotoDto> Upload(Guid orderId, [FromForm] UploadPhotoForm form, CancellationToken ct) =>
        photos.UploadAsync(orderId, form.Type, form.File, ct);

    [HttpGet]
    public Task<IReadOnlyList<OrderPhotoDto>> Get(Guid orderId, CancellationToken ct) => photos.GetAsync(orderId, ct);
}
