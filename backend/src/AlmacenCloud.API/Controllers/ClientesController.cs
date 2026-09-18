using AlmacenCloud.Application.DTOs;
using AlmacenCloud.Application.Features.Sales;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AlmacenCloud.API.Controllers;

[ApiController, Authorize, Route("api/v1/clientes")]
public sealed class ClientesController(ISalesService service) : ControllerBase
{
    [HttpGet] public async Task<IActionResult> List([FromQuery] string? search, CancellationToken ct) => Ok(await service.GetClientesAsync(search, ct));
    [HttpGet("{id:guid}")] public async Task<IActionResult> Get(Guid id, CancellationToken ct) => Ok(await service.GetClienteAsync(id, ct));
    [HttpPost] public async Task<IActionResult> Create(ClienteRequest request, CancellationToken ct) => StatusCode(201, await service.CreateClienteAsync(request, ct));
    [HttpPut("{id:guid}")] public async Task<IActionResult> Update(Guid id, ClienteRequest request, CancellationToken ct) => Ok(await service.UpdateClienteAsync(id, request, ct));
    [HttpDelete("{id:guid}")] public async Task<IActionResult> Delete(Guid id, CancellationToken ct) { await service.DeleteClienteAsync(id, ct); return NoContent(); }
}
