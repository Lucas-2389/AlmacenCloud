using AlmacenCloud.Application.DTOs;
using AlmacenCloud.Application.Features.Inventory;
using AlmacenCloud.Domain.Enums;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AlmacenCloud.API.Controllers;

[ApiController, Authorize, Route("api/v1/inventario")]
public sealed class InventarioController(IInventoryCoreService service) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> List([FromQuery] Guid? almacenId, [FromQuery] Guid? productoId, [FromQuery] bool? stockBajo, CancellationToken ct) =>
        Ok(await service.GetInventariosAsync(almacenId, productoId, stockBajo, ct));

    [HttpGet("{almacenId:guid}/{productoId:guid}")]
    public async Task<IActionResult> Get(Guid almacenId, Guid productoId, CancellationToken ct) =>
        Ok(await service.GetInventarioAsync(almacenId, productoId, ct));

    [HttpPost("entrada")]
    public async Task<IActionResult> Entrada(MovimientoStockRequest request, CancellationToken ct) => Ok(await service.EntradaAsync(request, ct));

    [HttpPost("salida")]
    public async Task<IActionResult> Salida(MovimientoStockRequest request, CancellationToken ct) => Ok(await service.SalidaAsync(request, ct));

    [HttpPost("transferencia")]
    public async Task<IActionResult> Transferencia(TransferenciaRequest request, CancellationToken ct)
    {
        await service.TransferenciaAsync(request, ct);
        return NoContent();
    }

    [HttpGet("movimientos")]
    public async Task<IActionResult> Movimientos([FromQuery] Guid? productoId, [FromQuery] Guid? almacenId,
        [FromQuery] TipoMovimiento? tipo, [FromQuery] DateTime? desde, [FromQuery] DateTime? hasta,
        [FromQuery] int page = 1, [FromQuery] int pageSize = 20, CancellationToken ct = default) =>
        Ok(await service.GetMovimientosAsync(productoId, almacenId, tipo, desde, hasta, page, pageSize, ct));
}
