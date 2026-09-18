using AlmacenCloud.Application.DTOs;
using AlmacenCloud.Application.Features.Inventory;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AlmacenCloud.API.Controllers;

[ApiController, Authorize, Route("api/v1/almacenes")]
public sealed class AlmacenesController(IInventoryCoreService service) : ControllerBase
{
    [HttpGet] public async Task<IActionResult> List(CancellationToken ct) => Ok(await service.GetAlmacenesAsync(ct));
    [HttpGet("{id:guid}")] public async Task<IActionResult> Get(Guid id, CancellationToken ct) => Ok(await service.GetAlmacenAsync(id, ct));
    [HttpPost] public async Task<IActionResult> Create(AlmacenRequest request, CancellationToken ct) => StatusCode(201, await service.CreateAlmacenAsync(request, ct));
    [HttpPut("{id:guid}")] public async Task<IActionResult> Update(Guid id, AlmacenRequest request, CancellationToken ct) => Ok(await service.UpdateAlmacenAsync(id, request, ct));
    [HttpDelete("{id:guid}")] public async Task<IActionResult> Delete(Guid id, CancellationToken ct) { await service.DeleteAlmacenAsync(id, ct); return NoContent(); }
}
