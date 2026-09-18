using AlmacenCloud.Application.DTOs;
using AlmacenCloud.Application.Features.Sales;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AlmacenCloud.API.Controllers;

[ApiController, Authorize, Route("api/v1/proveedores")]
public sealed class ProveedoresController(ISalesService service) : ControllerBase
{
    [HttpGet] public async Task<IActionResult> List([FromQuery] string? search, CancellationToken ct) => Ok(await service.GetProveedoresAsync(search, ct));
    [HttpGet("{id:guid}")] public async Task<IActionResult> Get(Guid id, CancellationToken ct) => Ok(await service.GetProveedorAsync(id, ct));
    [HttpPost] public async Task<IActionResult> Create(ProveedorRequest request, CancellationToken ct) => StatusCode(201, await service.CreateProveedorAsync(request, ct));
    [HttpPut("{id:guid}")] public async Task<IActionResult> Update(Guid id, ProveedorRequest request, CancellationToken ct) => Ok(await service.UpdateProveedorAsync(id, request, ct));
    [HttpDelete("{id:guid}")] public async Task<IActionResult> Delete(Guid id, CancellationToken ct) { await service.DeleteProveedorAsync(id, ct); return NoContent(); }
}
