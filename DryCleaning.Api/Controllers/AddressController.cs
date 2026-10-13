using DryCleaning.Api.Common;
using DryCleaning.Api.Dtos;
using DryCleaning.Api.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace DryCleaning.Api.Controllers;

/// <summary>The signed-in user's single address (Google Places fields). V1: one address per role.</summary>
[ApiController]
[Route("api/me/address")]
[Authorize(Roles = $"{Roles.Customer},{Roles.Provider},{Roles.Driver}")]
[Produces("application/json")]
public sealed class AddressController(AddressService addresses) : ControllerBase
{
    /// <summary>Get my current address (204 when none is set).</summary>
    [HttpGet]
    [ProducesResponseType<AddressDto>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> Get(CancellationToken ct)
    {
        var address = await addresses.GetMineAsync(ct);
        return address is null ? NoContent() : Ok(address);
    }

    /// <summary>Create or replace my address from a Google Places selection.</summary>
    [HttpPut]
    public Task<AddressDto> Upsert(UpsertAddressRequest request, CancellationToken ct) => addresses.UpsertMineAsync(request, ct);
}
