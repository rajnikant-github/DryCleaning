using DryCleaning.Api.Common;
using DryCleaning.Api.Dtos;
using DryCleaning.Api.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace DryCleaning.Api.Controllers;

/// <summary>My profile photo. Shown in the app's avatar; drivers' photos are also shown to the customer on tracking.</summary>
[ApiController]
[Route("api/me/photo")]
[Authorize(Roles = $"{Roles.Customer},{Roles.Provider},{Roles.Driver}")]
[Produces("application/json")]
public sealed class UserPhotoController(UserPhotoService photos) : ControllerBase
{
    /// <summary>Upload or replace my profile photo (JPG, PNG, HEIC or WEBP). Returns my updated user.</summary>
    [HttpPost]
    [Consumes("multipart/form-data")]
    [RequestSizeLimit(10 * 1024 * 1024)]
    public Task<UserDto> Upload([FromForm] UploadFileForm form, CancellationToken ct) => photos.UploadAsync(form.File, ct);

    /// <summary>Remove my profile photo (back to the initials avatar). Returns my updated user.</summary>
    [HttpDelete]
    public Task<UserDto> Remove(CancellationToken ct) => photos.RemoveAsync(ct);
}
