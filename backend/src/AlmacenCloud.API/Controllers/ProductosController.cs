using AlmacenCloud.Application.DTOs;
using AlmacenCloud.Application.Features.Inventory;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AlmacenCloud.API.Controllers;

[ApiController, Authorize, Route("api/v1/productos")]
public sealed class ProductosController(IInventoryCoreService service) : ControllerBase
{
    [HttpGet] public async Task<IActionResult> List([FromQuery] string? search, CancellationToken ct) => Ok(await service.GetProductosAsync(search, ct));
    [HttpGet("{id:guid}")] public async Task<IActionResult> Get(Guid id, CancellationToken ct) => Ok(await service.GetProductoAsync(id, ct));
    [HttpPost] public async Task<IActionResult> Create(ProductoRequest request, CancellationToken ct) => StatusCode(201, await service.CreateProductoAsync(request, ct));
    [HttpPut("{id:guid}")] public async Task<IActionResult> Update(Guid id, ProductoRequest request, CancellationToken ct) => Ok(await service.UpdateProductoAsync(id, request, ct));
    [HttpDelete("{id:guid}")] public async Task<IActionResult> Delete(Guid id, CancellationToken ct) { await service.DeleteProductoAsync(id, ct); return NoContent(); }
    [HttpPost("{id:guid}/imagen")]
    [RequestSizeLimit(5 * 1024 * 1024)]
    public async Task<IActionResult> UploadImage(Guid id, IFormFile imagen, CancellationToken ct)
    {
        await using var stream = imagen.OpenReadStream();
        return Ok(await service.UploadProductoImageAsync(id, stream, imagen.FileName, imagen.ContentType, imagen.Length, ct));
    }
    [HttpDelete("{id:guid}/imagen")]
    public async Task<IActionResult> DeleteImage(Guid id, CancellationToken ct) => Ok(await service.DeleteProductoImageAsync(id, ct));
}
