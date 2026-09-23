using AlmacenCloud.Application.DTOs;
using AlmacenCloud.Application.Features.Purchasing;
using AlmacenCloud.Domain.Enums;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AlmacenCloud.API.Controllers;

[ApiController, Authorize, Route("api/v1/compras")]
public sealed class ComprasController(IPurchasingService service) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> List([FromQuery] DateTime? desde, [FromQuery] DateTime? hasta,
        [FromQuery] Guid? proveedorId, [FromQuery] Guid? almacenId, [FromQuery] EstadoCompra? estado,
        [FromQuery] int page = 1, [FromQuery] int pageSize = 20, CancellationToken ct = default) =>
        Ok(await service.GetComprasAsync(desde, hasta, proveedorId, almacenId, estado, page, pageSize, ct));

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> Get(Guid id, CancellationToken ct) => Ok(await service.GetCompraAsync(id, ct));

    [HttpPost]
    public async Task<IActionResult> Create(CreateCompraRequest request, CancellationToken ct)
    {
        var purchase = await service.CreateCompraAsync(request, ct);
        return CreatedAtAction(nameof(Get), new { id = purchase.Id }, purchase);
    }

    [HttpPost("{id:guid}/anular")]
    public async Task<IActionResult> Annul(Guid id, CancellationToken ct) => Ok(await service.AnnulCompraAsync(id, ct));
}
