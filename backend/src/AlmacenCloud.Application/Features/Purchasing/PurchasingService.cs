using AlmacenCloud.Application.Abstractions;
using AlmacenCloud.Application.Common;
using AlmacenCloud.Application.DTOs;
using AlmacenCloud.Domain.Entities;
using AlmacenCloud.Domain.Enums;

namespace AlmacenCloud.Application.Features.Purchasing;

public sealed class PurchasingService(IPurchasingRepository repository, IInventoryCoreRepository inventoryRepository,
    ISalesTaxCalculator taxCalculator, ICurrentUser currentUser) : IPurchasingService
{
    private const int MaxAttempts = 4;

    public async Task<CompraResponse> CreateCompraAsync(CreateCompraRequest request, CancellationToken ct)
    {
        var (empresaId, usuarioId) = Context();
        Validate(request);
        var orderedItems = request.Items.OrderBy(x => x.ProductoId).ToArray();

        for (var attempt = 1; attempt <= MaxAttempts; attempt++)
        {
            await using var transaction = await repository.BeginTransactionAsync(ct);
            try
            {
                var supplier = await repository.GetProveedorAsync(request.ProveedorId, ct)
                    ?? throw new NotFoundException("Proveedor no encontrado.");
                var warehouse = await inventoryRepository.GetAlmacenAsync(request.AlmacenId, ct)
                    ?? throw new NotFoundException("Almacén no encontrado.");
                var products = new List<Producto>();
                var inventories = new List<Inventario?>();
                foreach (var item in orderedItems)
                {
                    var product = await inventoryRepository.GetProductoAsync(item.ProductoId, ct)
                        ?? throw new NotFoundException("Producto no encontrado.");
                    products.Add(product);
                    inventories.Add(await inventoryRepository.GetInventarioSnapshotAsync(warehouse.Id, product.Id, ct));
                }

                var sequence = await repository.GetSequenceSnapshotAsync(ct);
                long nextNumber;
                if (sequence is null) { nextNumber = 1; repository.Add(SecuenciaCompra.Create(empresaId)); }
                else
                {
                    nextNumber = sequence.UltimoNumero + 1;
                    if (!await repository.TryAdvanceSequenceAsync(sequence.Version, ct))
                    { await transaction.RollbackAsync(ct); Clear(); continue; }
                }

                var amounts = orderedItems.Select((item, index) =>
                    taxCalculator.Calculate(item.Cantidad, item.PrecioUnitario, products[index].AfectoIgv)).ToArray();
                var purchase = Compra.Create(empresaId, supplier.Id, warehouse.Id, usuarioId, $"C{nextNumber:00000000}",
                    amounts.Sum(x => x.Subtotal), amounts.Sum(x => x.Igv), amounts.Sum(x => x.Total),
                    request.NumeroDocumentoProveedor, request.Observacion);

                for (var index = 0; index < orderedItems.Length; index++)
                {
                    var item = orderedItems[index]; var product = products[index]; var inventory = inventories[index];
                    var before = inventory?.Cantidad ?? 0m; var after = before + item.Cantidad;
                    if (inventory is null) inventoryRepository.Add(Inventario.Create(empresaId, warehouse.Id, product.Id, after));
                    else if (!await inventoryRepository.TrySetQuantityAsync(inventory.Id, inventory.Version, after, ct))
                    { await transaction.RollbackAsync(ct); Clear(); goto RetryPurchase; }

                    purchase.AddDetail(CompraDetalle.Create(purchase.Id, empresaId, product.Id, product.Codigo, product.Nombre,
                        product.UnidadMedida, item.Cantidad, item.PrecioUnitario, amounts[index].Subtotal, amounts[index].Igv, amounts[index].Total));
                    inventoryRepository.Add(MovimientoInventario.Create(empresaId, warehouse.Id, product.Id, usuarioId,
                        TipoMovimiento.CompraEntrada, item.Cantidad, before, after, $"Compra {purchase.Numero}", purchase.Numero, compraId: purchase.Id));
                }

                repository.Add(purchase);
                await repository.SaveChangesAsync(ct);
                await transaction.CommitAsync(ct);
                Clear();
                return await GetCompraAsync(purchase.Id, ct);

                RetryPurchase:;
            }
            catch (Exception exception)
            {
                await transaction.RollbackAsync(ct); Clear();
                if (attempt < MaxAttempts && repository.IsTransient(exception))
                { await Task.Delay(attempt * 25, ct); continue; }
                throw;
            }
        }

        throw new ConflictException("No fue posible registrar la compra debido a operaciones concurrentes. Intente nuevamente.");
    }

    public async Task<PagedResponse<CompraListResponse>> GetComprasAsync(DateTime? desde, DateTime? hasta,
        Guid? proveedorId, Guid? almacenId, EstadoCompra? estado, int page, int pageSize, CancellationToken ct)
    {
        _ = Tenant(); page = Math.Max(1, page); pageSize = Math.Clamp(pageSize, 1, 100);
        var result = await repository.GetComprasAsync(desde, hasta, proveedorId, almacenId, estado, page, pageSize, ct);
        return new(result.Items.Select(MapList).ToArray(), page, pageSize, result.Total);
    }

    public async Task<CompraResponse> GetCompraAsync(Guid id, CancellationToken ct) =>
        Map(await repository.GetCompraAsync(id, ct) ?? throw new NotFoundException("Compra no encontrada."));

    public async Task<CompraResponse> AnnulCompraAsync(Guid id, CancellationToken ct)
    {
        var (empresaId, usuarioId) = Context();
        for (var attempt = 1; attempt <= MaxAttempts; attempt++)
        {
            await using var transaction = await repository.BeginTransactionAsync(ct);
            try
            {
                var purchase = await repository.GetCompraSnapshotAsync(id, ct)
                    ?? throw new NotFoundException("Compra no encontrada.");
                if (purchase.Estado == EstadoCompra.Anulada) throw new BusinessRuleException("La compra ya está anulada.");
                var details = purchase.Detalles.OrderBy(x => x.ProductoId).ToArray();
                var inventories = new List<Inventario>();
                foreach (var detail in details)
                {
                    var inventory = await inventoryRepository.GetInventarioSnapshotAsync(purchase.AlmacenId, detail.ProductoId, ct)
                        ?? throw new BusinessRuleException("No existe inventario suficiente para anular la compra.");
                    if (inventory.Cantidad < detail.Cantidad)
                        throw new BusinessRuleException($"Stock insuficiente para revertir el producto {detail.NombreProducto}.");
                    inventories.Add(inventory);
                }

                for (var index = 0; index < details.Length; index++)
                {
                    var detail = details[index]; var inventory = inventories[index]; var after = inventory.Cantidad - detail.Cantidad;
                    if (!await inventoryRepository.TrySetQuantityAsync(inventory.Id, inventory.Version, after, ct))
                    { await transaction.RollbackAsync(ct); Clear(); goto RetryAnnulment; }
                    inventoryRepository.Add(MovimientoInventario.Create(empresaId, purchase.AlmacenId, detail.ProductoId, usuarioId,
                        TipoMovimiento.AnulacionCompraSalida, detail.Cantidad, inventory.Cantidad, after,
                        $"Anulación compra {purchase.Numero}", purchase.Numero, compraId: purchase.Id));
                }

                if (!await repository.TryMarkPurchaseAnnulledAsync(purchase.Id, purchase.Version, usuarioId, ct))
                { await transaction.RollbackAsync(ct); Clear(); goto RetryAnnulment; }
                await repository.SaveChangesAsync(ct);
                await transaction.CommitAsync(ct);
                Clear();
                return await GetCompraAsync(id, ct);

                RetryAnnulment:;
            }
            catch (Exception exception)
            {
                await transaction.RollbackAsync(ct); Clear();
                if (attempt < MaxAttempts && repository.IsTransient(exception))
                { await Task.Delay(attempt * 25, ct); continue; }
                throw;
            }
        }

        throw new ConflictException("No fue posible anular la compra debido a operaciones concurrentes. Intente nuevamente.");
    }

    private static void Validate(CreateCompraRequest request)
    {
        if (request.Items is null || request.Items.Count == 0) throw new ValidationException("La compra debe contener al menos un producto.");
        if (request.Items.GroupBy(x => x.ProductoId).Any(x => x.Count() > 1)) throw new ValidationException("No se permiten productos duplicados en la compra.");
        if (request.Items.Any(x => x.Cantidad <= 0)) throw new ValidationException("Las cantidades deben ser mayores que cero.");
        if (request.Items.Any(x => x.PrecioUnitario < 0)) throw new ValidationException("Los precios no pueden ser negativos.");
        if (request.NumeroDocumentoProveedor?.Trim().Length > 100) throw new ValidationException("El documento del proveedor no puede exceder 100 caracteres.");
        if (request.Observacion?.Trim().Length > 1000) throw new ValidationException("La observación no puede exceder 1000 caracteres.");
    }

    private Guid Tenant() => currentUser.EmpresaId ?? throw new AuthenticationException("No existe un tenant válido.");
    private (Guid, Guid) Context() => (Tenant(), currentUser.UsuarioId ?? throw new AuthenticationException("No existe un usuario válido."));
    private void Clear() { repository.ClearTracking(); inventoryRepository.ClearTracking(); }
    private static CompraListResponse MapList(Compra x) => new(x.Id, x.Numero, x.Fecha, x.Estado,
        x.Proveedor.RazonSocial, x.Almacen.Nombre, x.NumeroDocumentoProveedor, x.Total);
    private static CompraResponse Map(Compra x) => new(x.Id, x.Numero, x.Fecha, x.Estado, x.ProveedorId,
        x.Proveedor.RazonSocial, x.AlmacenId, x.Almacen.Nombre, x.UsuarioId, $"{x.Usuario.Nombre} {x.Usuario.Apellidos}",
        x.NumeroDocumentoProveedor, x.Subtotal, x.Igv, x.Total, x.Observacion, x.AnuladoPorUsuarioId, x.AnuladoEn,
        x.Detalles.Select(d => new CompraDetalleResponse(d.Id, d.ProductoId, d.CodigoProducto, d.NombreProducto,
            d.UnidadMedida, d.Cantidad, d.PrecioUnitario, d.Subtotal, d.Igv, d.Total)).ToArray());
}
