using AlmacenCloud.Application.Abstractions;
using AlmacenCloud.Application.Common;
using AlmacenCloud.Application.DTOs;
using AlmacenCloud.Domain.Entities;
using AlmacenCloud.Domain.Enums;

namespace AlmacenCloud.Application.Features.Sales;

public sealed class SalesService(ISalesRepository repository, IInventoryCoreRepository inventoryRepository,
    ISalesTaxCalculator taxCalculator, ICurrentUser currentUser) : ISalesService
{
    private const int MaxAttempts = 4;

    public async Task<IReadOnlyCollection<ClienteResponse>> GetClientesAsync(string? search, CancellationToken ct) =>
        (await repository.GetClientesAsync(search, ct)).Select(Map).ToArray();
    public async Task<ClienteResponse> GetClienteAsync(Guid id, CancellationToken ct) => Map(await Client(id, ct));
    public async Task<ClienteResponse> CreateClienteAsync(ClienteRequest request, CancellationToken ct)
    {
        var document = Normalize(request.NumeroDocumento);
        if (await repository.ClienteDocumentExistsAsync(request.TipoDocumento, document, null, ct)) throw new ConflictException("El cliente ya está registrado.");
        var entity = Cliente.Create(Tenant(), request.TipoDocumento, document, request.NombreRazonSocial, request.Direccion, request.Telefono, request.Email);
        repository.Add(entity); await repository.SaveChangesAsync(ct); return Map(entity);
    }
    public async Task<ClienteResponse> UpdateClienteAsync(Guid id, ClienteRequest request, CancellationToken ct)
    {
        var entity = await Client(id, ct); var document = Normalize(request.NumeroDocumento);
        if (await repository.ClienteDocumentExistsAsync(request.TipoDocumento, document, id, ct)) throw new ConflictException("El cliente ya está registrado.");
        entity.Update(request.TipoDocumento, document, request.NombreRazonSocial, request.Direccion, request.Telefono, request.Email);
        await repository.SaveChangesAsync(ct); return Map(entity);
    }
    public async Task DeleteClienteAsync(Guid id, CancellationToken ct) { var entity = await Client(id, ct); entity.Deactivate(); await repository.SaveChangesAsync(ct); }

    public async Task<IReadOnlyCollection<ProveedorResponse>> GetProveedoresAsync(string? search, CancellationToken ct) =>
        (await repository.GetProveedoresAsync(search, ct)).Select(Map).ToArray();
    public async Task<ProveedorResponse> GetProveedorAsync(Guid id, CancellationToken ct) => Map(await Supplier(id, ct));
    public async Task<ProveedorResponse> CreateProveedorAsync(ProveedorRequest request, CancellationToken ct)
    {
        var ruc = Normalize(request.Ruc);
        if (await repository.ProveedorRucExistsAsync(ruc, null, ct)) throw new ConflictException("El proveedor ya está registrado.");
        var entity = Proveedor.Create(Tenant(), ruc, request.RazonSocial, request.NombreComercial, request.Direccion, request.Telefono, request.Email);
        repository.Add(entity); await repository.SaveChangesAsync(ct); return Map(entity);
    }
    public async Task<ProveedorResponse> UpdateProveedorAsync(Guid id, ProveedorRequest request, CancellationToken ct)
    {
        var entity = await Supplier(id, ct); var ruc = Normalize(request.Ruc);
        if (await repository.ProveedorRucExistsAsync(ruc, id, ct)) throw new ConflictException("El proveedor ya está registrado.");
        entity.Update(ruc, request.RazonSocial, request.NombreComercial, request.Direccion, request.Telefono, request.Email);
        await repository.SaveChangesAsync(ct); return Map(entity);
    }
    public async Task DeleteProveedorAsync(Guid id, CancellationToken ct) { var entity = await Supplier(id, ct); entity.Deactivate(); await repository.SaveChangesAsync(ct); }

    public async Task<VentaResponse> CreateVentaAsync(CreateVentaRequest request, CancellationToken ct)
    {
        var (empresaId, usuarioId) = Context();
        ValidateSaleRequest(request);
        var orderedItems = request.Items.OrderBy(x => x.ProductoId).ToArray();

        for (var attempt = 1; attempt <= MaxAttempts; attempt++)
        {
            await using var transaction = await repository.BeginTransactionAsync(ct);
            try
            {
                var client = request.ClienteId.HasValue ? await Client(request.ClienteId.Value, ct) : null;
                var warehouse = await inventoryRepository.GetAlmacenAsync(request.AlmacenId, ct) ?? throw new NotFoundException("Almacén no encontrado.");
                var products = new List<Producto>();
                var inventories = new List<Inventario>();
                foreach (var item in orderedItems)
                {
                    var product = await inventoryRepository.GetProductoAsync(item.ProductoId, ct) ?? throw new NotFoundException("Producto no encontrado.");
                    var inventory = await inventoryRepository.GetInventarioSnapshotAsync(warehouse.Id, product.Id, ct)
                        ?? throw new BusinessRuleException($"Stock insuficiente para {product.Nombre}.");
                    if (inventory.Cantidad < item.Cantidad) throw new BusinessRuleException($"Stock insuficiente para {product.Nombre}.");
                    products.Add(product); inventories.Add(inventory);
                }

                var sequence = await repository.GetSequenceSnapshotAsync(ct);
                long nextNumber;
                if (sequence is null) { nextNumber = 1; repository.Add(SecuenciaVenta.Create(empresaId)); }
                else
                {
                    nextNumber = sequence.UltimoNumero + 1;
                    if (!await repository.TryAdvanceSequenceAsync(sequence.Version, ct)) { await transaction.RollbackAsync(ct); Clear(); continue; }
                }

                var lineAmounts = orderedItems.Select((item, index) => taxCalculator.Calculate(item.Cantidad, item.PrecioUnitario, products[index].AfectoIgv)).ToArray();
                var subtotal = lineAmounts.Sum(x => x.Subtotal); var igv = lineAmounts.Sum(x => x.Igv); var total = lineAmounts.Sum(x => x.Total);
                var sale = Venta.Create(empresaId, client?.Id, client?.NombreRazonSocial ?? "CONSUMIDOR FINAL", warehouse.Id, usuarioId,
                    $"V{nextNumber:00000000}", subtotal, igv, total, request.Observacion);

                for (var index = 0; index < orderedItems.Length; index++)
                {
                    var item = orderedItems[index]; var product = products[index]; var inventory = inventories[index]; var amounts = lineAmounts[index];
                    var after = inventory.Cantidad - item.Cantidad;
                    if (!await inventoryRepository.TrySetQuantityAsync(inventory.Id, inventory.Version, after, ct))
                    { await transaction.RollbackAsync(ct); Clear(); goto RetrySale; }
                    sale.AddDetail(VentaDetalle.Create(sale.Id, empresaId, product.Id, product.Codigo, product.Nombre, product.UnidadMedida,
                        item.Cantidad, item.PrecioUnitario, amounts.Subtotal, amounts.Igv, amounts.Total));
                    inventoryRepository.Add(MovimientoInventario.Create(empresaId, warehouse.Id, product.Id, usuarioId, TipoMovimiento.Salida,
                        item.Cantidad, inventory.Cantidad, after, $"Venta {sale.Numero}", sale.Numero, ventaId: sale.Id));
                }
                repository.Add(sale); await repository.SaveChangesAsync(ct); await transaction.CommitAsync(ct);
                Clear(); return await GetVentaAsync(sale.Id, ct);

                RetrySale:;
            }
            catch (Exception exception)
            {
                await transaction.RollbackAsync(ct); Clear();
                if (attempt < MaxAttempts && repository.IsTransient(exception)) { await Task.Delay(attempt * 25, ct); continue; }
                throw;
            }
        }
        throw new ConflictException("No fue posible registrar la venta debido a operaciones concurrentes. Intente nuevamente.");
    }

    public async Task<PagedResponse<VentaListResponse>> GetVentasAsync(DateTime? desde, DateTime? hasta, Guid? clienteId, Guid? almacenId, int page, int pageSize, CancellationToken ct)
    {
        _ = Tenant(); page = Math.Max(1, page); pageSize = Math.Clamp(pageSize, 1, 100);
        var result = await repository.GetVentasAsync(desde, hasta, clienteId, almacenId, page, pageSize, ct);
        return new(result.Items.Select(MapList).ToArray(), page, pageSize, result.Total);
    }
    public async Task<VentaResponse> GetVentaAsync(Guid id, CancellationToken ct) => Map(await repository.GetVentaAsync(id, ct) ?? throw new NotFoundException("Venta no encontrada."));

    public async Task<VentaResponse> AnnulVentaAsync(Guid id, CancellationToken ct)
    {
        var (empresaId, usuarioId) = Context();
        for (var attempt = 1; attempt <= MaxAttempts; attempt++)
        {
            await using var transaction = await repository.BeginTransactionAsync(ct);
            try
            {
                var sale = await repository.GetVentaSnapshotAsync(id, ct) ?? throw new NotFoundException("Venta no encontrada.");
                if (sale.Estado == EstadoVenta.Anulada) throw new BusinessRuleException("La venta ya está anulada.");
                var details = sale.Detalles.OrderBy(x => x.ProductoId).ToArray();
                var inventories = new List<Inventario>();
                foreach (var detail in details)
                    inventories.Add(await inventoryRepository.GetInventarioSnapshotAsync(sale.AlmacenId, detail.ProductoId, ct)
                        ?? throw new BusinessRuleException("No existe inventario para reponer la venta."));
                for (var index = 0; index < details.Length; index++)
                {
                    var detail = details[index]; var inventory = inventories[index]; var after = inventory.Cantidad + detail.Cantidad;
                    if (!await inventoryRepository.TrySetQuantityAsync(inventory.Id, inventory.Version, after, ct))
                    { await transaction.RollbackAsync(ct); Clear(); goto RetryAnnulment; }
                    inventoryRepository.Add(MovimientoInventario.Create(empresaId, sale.AlmacenId, detail.ProductoId, usuarioId,
                        TipoMovimiento.AjusteEntrada, detail.Cantidad, inventory.Cantidad, after, $"Anulación venta {sale.Numero}", sale.Numero, ventaId: sale.Id));
                }
                if (!await repository.TryMarkSaleAnnulledAsync(sale.Id, sale.Version, usuarioId, ct))
                { await transaction.RollbackAsync(ct); Clear(); goto RetryAnnulment; }
                await repository.SaveChangesAsync(ct); await transaction.CommitAsync(ct); Clear(); return await GetVentaAsync(id, ct);

                RetryAnnulment:;
            }
            catch (Exception exception)
            {
                await transaction.RollbackAsync(ct); Clear();
                if (attempt < MaxAttempts && repository.IsTransient(exception)) { await Task.Delay(attempt * 25, ct); continue; }
                throw;
            }
        }
        throw new ConflictException("No fue posible anular la venta debido a operaciones concurrentes. Intente nuevamente.");
    }

    private static void ValidateSaleRequest(CreateVentaRequest request)
    {
        if (request.Items is null || request.Items.Count == 0) throw new ValidationException("La venta debe contener al menos un producto.");
        if (request.Items.GroupBy(x => x.ProductoId).Any(x => x.Count() > 1)) throw new ValidationException("No se permiten productos duplicados en la venta.");
        if (request.Items.Any(x => x.Cantidad <= 0)) throw new ValidationException("Las cantidades deben ser mayores que cero.");
        if (request.Items.Any(x => x.PrecioUnitario < 0)) throw new ValidationException("Los precios no pueden ser negativos.");
    }
    private async Task<Cliente> Client(Guid id, CancellationToken ct) => await repository.GetClienteAsync(id, ct) ?? throw new NotFoundException("Cliente no encontrado.");
    private async Task<Proveedor> Supplier(Guid id, CancellationToken ct) => await repository.GetProveedorAsync(id, ct) ?? throw new NotFoundException("Proveedor no encontrado.");
    private Guid Tenant() => currentUser.EmpresaId ?? throw new AuthenticationException("No existe un tenant válido.");
    private (Guid, Guid) Context() => (Tenant(), currentUser.UsuarioId ?? throw new AuthenticationException("No existe un usuario válido."));
    private void Clear() { repository.ClearTracking(); inventoryRepository.ClearTracking(); }
    private static string Normalize(string value) => string.IsNullOrWhiteSpace(value) ? throw new ValidationException("El valor es obligatorio.") : value.Trim().ToUpperInvariant();
    private static ClienteResponse Map(Cliente x) => new(x.Id, x.TipoDocumento, x.NumeroDocumento, x.NombreRazonSocial, x.Direccion, x.Telefono, x.Email, x.Activo);
    private static ProveedorResponse Map(Proveedor x) => new(x.Id, x.Ruc, x.RazonSocial, x.NombreComercial, x.Direccion, x.Telefono, x.Email, x.Activo);
    private static VentaListResponse MapList(Venta x) => new(x.Id, x.Numero, x.Fecha, x.Estado, x.ClienteNombre, x.Almacen.Nombre, x.Total);
    private static VentaResponse Map(Venta x) => new(x.Id, x.Numero, x.Fecha, x.Estado, x.ClienteId, x.ClienteNombre,
        x.AlmacenId, x.Almacen.Nombre, x.UsuarioId, $"{x.Usuario.Nombre} {x.Usuario.Apellidos}", x.Subtotal, x.Igv, x.Total,
        x.Observacion, x.AnuladaPorUsuarioId, x.AnuladaEn, x.Detalles.Select(d => new VentaDetalleResponse(d.Id, d.ProductoId,
            d.CodigoProducto, d.NombreProducto, d.UnidadMedida, d.Cantidad, d.PrecioUnitario, d.Subtotal, d.Igv, d.Total)).ToArray());
}
