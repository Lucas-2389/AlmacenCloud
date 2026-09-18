using AlmacenCloud.Application.DTOs;
using AlmacenCloud.Application.Features.Inventory;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AlmacenCloud.API.Controllers;

[ApiController, Authorize, Route("api/v1/categorias")]
public sealed class CategoriasController(IInventoryCoreService service) : ControllerBase
{
    [HttpGet] public async Task<IActionResult> List(CancellationToken ct) => Ok(await service.GetCategoriasAsync(ct));
    [HttpGet("{id:guid}")] public async Task<IActionResult> Get(Guid id, CancellationToken ct) => Ok(await service.GetCategoriaAsync(id, ct));
    [HttpPost] public async Task<IActionResult> Create(CategoriaRequest request, CancellationToken ct) => StatusCode(201, await service.CreateCategoriaAsync(request, ct));
    [HttpPut("{id:guid}")] public async Task<IActionResult> Update(Guid id, CategoriaRequest request, CancellationToken ct) => Ok(await service.UpdateCategoriaAsync(id, request, ct));
    [HttpDelete("{id:guid}")] public async Task<IActionResult> Delete(Guid id, CancellationToken ct) { await service.DeleteCategoriaAsync(id, ct); return NoContent(); }
}
