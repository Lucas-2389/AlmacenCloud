using AlmacenCloud.Application.DTOs;
using AlmacenCloud.Application.Features.Sales;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AlmacenCloud.API.Controllers;

[ApiController, Authorize, Route("api/v1/ventas")]
public sealed class VentasController(ISalesService service) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> List([FromQuery] DateTime? desde, [FromQuery] DateTime? hasta, [FromQuery] Guid? clienteId,
        [FromQuery] Guid? almacenId, [FromQuery] int page = 1, [FromQuery] int pageSize = 20, CancellationToken ct = default) =>
        Ok(await service.GetVentasAsync(desde, hasta, clienteId, almacenId, page, pageSize, ct));

    [HttpGet("{id:guid}")] public async Task<IActionResult> Get(Guid id, CancellationToken ct) => Ok(await service.GetVentaAsync(id, ct));
    [HttpPost] public async Task<IActionResult> Create(CreateVentaRequest request, CancellationToken ct)
    {
        var sale = await service.CreateVentaAsync(request, ct);
        return CreatedAtAction(nameof(Get), new { id = sale.Id }, sale);
    }
    [HttpPost("{id:guid}/anular")] public async Task<IActionResult> Annul(Guid id, CancellationToken ct) => Ok(await service.AnnulVentaAsync(id, ct));
}
