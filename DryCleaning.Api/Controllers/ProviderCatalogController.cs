using DryCleaning.Api.Common;
using DryCleaning.Api.Dtos;
using DryCleaning.Api.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace DryCleaning.Api.Controllers;

/// <summary>P11 – Products and Service Items (Category → Product → Service Item).</summary>
[ApiController]
[Route("api/provider")]
[Authorize(Roles = Roles.Provider)]
[Produces("application/json")]
public sealed class ProviderCatalogController(ProviderCatalogService catalog) : ControllerBase
{
    [HttpGet("products")]
    public Task<IReadOnlyList<ProductDto>> GetProducts([FromQuery] Guid? categoryId, CancellationToken ct) => catalog.GetProductsAsync(categoryId, ct);

    [HttpGet("products/{productId:guid}")]
    public Task<ProductDto> GetProduct(Guid productId, CancellationToken ct) => catalog.GetProductAsync(productId, ct);

    [HttpPost("products")]
    public Task<ProductDto> CreateProduct(ProductRequest request, CancellationToken ct) => catalog.CreateProductAsync(request, ct);

    [HttpPut("products/{productId:guid}")]
    public Task<ProductDto> UpdateProduct(Guid productId, ProductRequest request, CancellationToken ct) => catalog.UpdateProductAsync(productId, request, ct);

    /// <summary>Enable / disable a product.</summary>
    [HttpPut("products/{productId:guid}/active")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> SetProductActive(Guid productId, SetActiveRequest request, CancellationToken ct)
    {
        await catalog.SetProductActiveAsync(productId, request.IsActive, ct);
        return NoContent();
    }

    [HttpPost("products/{productId:guid}/service-items")]
    public Task<ServiceItemDto> CreateServiceItem(Guid productId, ServiceItemRequest request, CancellationToken ct) =>
        catalog.CreateServiceItemAsync(productId, request, ct);

    /// <summary>Edit name, price and estimated days.</summary>
    [HttpGet("service-items/{serviceItemId:guid}")]
    public Task<ServiceItemDto> GetServiceItem(Guid serviceItemId, CancellationToken ct) => catalog.GetServiceItemAsync(serviceItemId, ct);

    [HttpPut("service-items/{serviceItemId:guid}")]
    public Task<ServiceItemDto> UpdateServiceItem(Guid serviceItemId, ServiceItemRequest request, CancellationToken ct) =>
        catalog.UpdateServiceItemAsync(serviceItemId, request, ct);

    [HttpPut("service-items/{serviceItemId:guid}/active")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> SetServiceItemActive(Guid serviceItemId, SetActiveRequest request, CancellationToken ct)
    {
        await catalog.SetServiceItemActiveAsync(serviceItemId, request.IsActive, ct);
        return NoContent();
    }
}
