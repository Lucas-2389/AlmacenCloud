using AlmacenCloud.Application.Abstractions;
using AlmacenCloud.Application.Common;
using AlmacenCloud.Application.DTOs;
using AlmacenCloud.Domain.Entities;
using AlmacenCloud.Domain.Enums;

namespace AlmacenCloud.Application.Features.Inventory;

public sealed class InventoryCoreService(IInventoryCoreRepository repository, ICurrentUser currentUser) : IInventoryCoreService
{
    private const int MaxConcurrencyAttempts = 4;

    public async Task<IReadOnlyCollection<CategoriaResponse>> GetCategoriasAsync(CancellationToken ct) =>
        (await repository.GetCategoriasAsync(ct)).Select(Map).ToArray();

    public async Task<CategoriaResponse> GetCategoriaAsync(Guid id, CancellationToken ct) => Map(await Category(id, ct));

    public async Task<CategoriaResponse> CreateCategoriaAsync(CategoriaRequest request, CancellationToken ct)
    {
        var empresaId = Tenant();
        var key = Normalize(request.Nombre);
        if (await repository.CategoriaNameExistsAsync(key, null, ct)) throw new ConflictException("Ya existe una categoría activa con ese nombre.");
        var entity = Categoria.Create(empresaId, request.Nombre, request.Descripcion);
        repository.Add(entity);
        await repository.SaveChangesAsync(ct);
        return Map(entity);
    }

    public async Task<CategoriaResponse> UpdateCategoriaAsync(Guid id, CategoriaRequest request, CancellationToken ct)
    {
        var entity = await Category(id, ct);
        var key = Normalize(request.Nombre);
        if (await repository.CategoriaNameExistsAsync(key, id, ct)) throw new ConflictException("Ya existe una categoría activa con ese nombre.");
        entity.Update(request.Nombre, request.Descripcion);
        await repository.SaveChangesAsync(ct);
        return Map(entity);
    }

    public async Task DeleteCategoriaAsync(Guid id, CancellationToken ct)
    {
        var entity = await Category(id, ct);
        entity.Deactivate();
        await repository.SaveChangesAsync(ct);
    }

    public async Task<IReadOnlyCollection<ProductoResponse>> GetProductosAsync(string? search, CancellationToken ct) =>
        (await repository.GetProductosAsync(search, ct)).Select(Map).ToArray();

    public async Task<ProductoResponse> GetProductoAsync(Guid id, CancellationToken ct) => Map(await Product(id, ct));

    public async Task<ProductoResponse> CreateProductoAsync(ProductoRequest request, CancellationToken ct)
    {
        var empresaId = Tenant();
        _ = await Category(request.CategoriaId, ct);
        var code = Normalize(request.Codigo);
        if (await repository.ProductoCodeExistsAsync(code, null, ct)) throw new ConflictException("Ya existe un producto con ese código.");
        var entity = Producto.Create(empresaId, request.CategoriaId, code, request.Nombre, request.Descripcion,
            request.UnidadMedida, request.PrecioCompra, request.PrecioVenta, request.StockMinimo, request.AfectoIgv);
        repository.Add(entity);
        await repository.SaveChangesAsync(ct);
        return Map(await Product(entity.Id, ct));
    }

    public async Task<ProductoResponse> UpdateProductoAsync(Guid id, ProductoRequest request, CancellationToken ct)
    {
        var entity = await Product(id, ct);
        _ = await Category(request.CategoriaId, ct);
        var code = Normalize(request.Codigo);
        if (await repository.ProductoCodeExistsAsync(code, id, ct)) throw new ConflictException("Ya existe un producto con ese código.");
        entity.Update(request.CategoriaId, code, request.Nombre, request.Descripcion, request.UnidadMedida,
            request.PrecioCompra, request.PrecioVenta, request.StockMinimo, request.AfectoIgv);
        await repository.SaveChangesAsync(ct);
        repository.ClearTracking();
        return Map(await Product(id, ct));
    }

    public async Task DeleteProductoAsync(Guid id, CancellationToken ct)
    {
        var entity = await Product(id, ct);
        entity.Deactivate();
        await repository.SaveChangesAsync(ct);
    }

    public async Task<IReadOnlyCollection<AlmacenResponse>> GetAlmacenesAsync(CancellationToken ct) =>
        (await repository.GetAlmacenesAsync(ct)).Select(Map).ToArray();

    public async Task<AlmacenResponse> GetAlmacenAsync(Guid id, CancellationToken ct) => Map(await Warehouse(id, ct));

    public async Task<AlmacenResponse> CreateAlmacenAsync(AlmacenRequest request, CancellationToken ct)
    {
        var empresaId = Tenant();
        var code = Normalize(request.Codigo);
        if (await repository.AlmacenCodeExistsAsync(code, null, ct)) throw new ConflictException("Ya existe un almacén con ese código.");
        var entity = Almacen.Create(empresaId, code, request.Nombre, request.Direccion);
        repository.Add(entity);
        await repository.SaveChangesAsync(ct);
        return Map(entity);
    }

    public async Task<AlmacenResponse> UpdateAlmacenAsync(Guid id, AlmacenRequest request, CancellationToken ct)
    {
        var entity = await Warehouse(id, ct);
        var code = Normalize(request.Codigo);
        if (await repository.AlmacenCodeExistsAsync(code, id, ct)) throw new ConflictException("Ya existe un almacén con ese código.");
        entity.Update(code, request.Nombre, request.Direccion);
        await repository.SaveChangesAsync(ct);
        return Map(entity);
    }

    public async Task DeleteAlmacenAsync(Guid id, CancellationToken ct)
    {
        var entity = await Warehouse(id, ct);
        entity.Deactivate();
        await repository.SaveChangesAsync(ct);
    }

    public async Task<IReadOnlyCollection<InventarioResponse>> GetInventariosAsync(Guid? almacenId, Guid? productoId, bool? stockBajo, CancellationToken ct) =>
        (await repository.GetInventariosAsync(almacenId, productoId, stockBajo, ct)).Select(Map).ToArray();

    public async Task<InventarioResponse> GetInventarioAsync(Guid almacenId, Guid productoId, CancellationToken ct) =>
        Map(await repository.GetInventarioAsync(almacenId, productoId, ct) ?? throw new NotFoundException("Inventario no encontrado."));

    public Task<InventarioResponse> EntradaAsync(MovimientoStockRequest request, CancellationToken ct) =>
        ChangeStock(request, true, ct);

    public Task<InventarioResponse> SalidaAsync(MovimientoStockRequest request, CancellationToken ct) =>
        ChangeStock(request, false, ct);

    public async Task TransferenciaAsync(TransferenciaRequest request, CancellationToken ct)
    {
        var (empresaId, usuarioId) = Context();
        ValidateAmount(request.Cantidad, request.Motivo);
        if (request.AlmacenOrigenId == request.AlmacenDestinoId) throw new ValidationException("Los almacenes deben ser diferentes.");
        _ = await Product(request.ProductoId, ct);
        _ = await Warehouse(request.AlmacenOrigenId, ct);
        _ = await Warehouse(request.AlmacenDestinoId, ct);

        for (var attempt = 1; attempt <= MaxConcurrencyAttempts; attempt++)
        {
            await using var transaction = await repository.BeginTransactionAsync(ct);
            try
            {
                var source = await repository.GetInventarioSnapshotAsync(request.AlmacenOrigenId, request.ProductoId, ct)
                    ?? throw new BusinessRuleException("El almacén origen no tiene stock disponible.");
                if (source.Cantidad < request.Cantidad) throw new BusinessRuleException("Stock insuficiente para la transferencia.");
                var destination = await repository.GetInventarioSnapshotAsync(request.AlmacenDestinoId, request.ProductoId, ct);
                var sourceAfter = source.Cantidad - request.Cantidad;
                var destinationBefore = destination?.Cantidad ?? 0;
                var destinationAfter = destinationBefore + request.Cantidad;

                if (!await repository.TrySetQuantityAsync(source.Id, source.Version, sourceAfter, ct))
                {
                    await transaction.RollbackAsync(ct); repository.ClearTracking(); continue;
                }

                if (destination is null)
                    repository.Add(Inventario.Create(empresaId, request.AlmacenDestinoId, request.ProductoId, destinationAfter));
                else if (!await repository.TrySetQuantityAsync(destination.Id, destination.Version, destinationAfter, ct))
                {
                    await transaction.RollbackAsync(ct); repository.ClearTracking(); continue;
                }

                var transferenciaId = Guid.NewGuid();
                repository.Add(MovimientoInventario.Create(empresaId, request.AlmacenOrigenId, request.ProductoId, usuarioId,
                    TipoMovimiento.TransferenciaSalida, request.Cantidad, source.Cantidad, sourceAfter, request.Motivo, transferenciaId: transferenciaId));
                repository.Add(MovimientoInventario.Create(empresaId, request.AlmacenDestinoId, request.ProductoId, usuarioId,
                    TipoMovimiento.TransferenciaEntrada, request.Cantidad, destinationBefore, destinationAfter, request.Motivo, transferenciaId: transferenciaId));
                await repository.SaveChangesAsync(ct);
                await transaction.CommitAsync(ct);
                return;
            }
            catch
            {
                await transaction.RollbackAsync(ct);
                repository.ClearTracking();
                throw;
            }
        }
        throw new ConflictException("No fue posible completar la transferencia debido a operaciones concurrentes. Intente nuevamente.");
    }

    public async Task<PagedResponse<MovimientoResponse>> GetMovimientosAsync(Guid? productoId, Guid? almacenId, TipoMovimiento? tipo,
        DateTime? desde, DateTime? hasta, int page, int pageSize, CancellationToken ct)
    {
        _ = Tenant();
        page = Math.Max(1, page);
        pageSize = Math.Clamp(pageSize, 1, 100);
        var result = await repository.GetMovimientosAsync(productoId, almacenId, tipo, desde, hasta, page, pageSize, ct);
        return new PagedResponse<MovimientoResponse>(result.Items.Select(Map).ToArray(), page, pageSize, result.Total);
    }

    private async Task<InventarioResponse> ChangeStock(MovimientoStockRequest request, bool incoming, CancellationToken ct)
    {
        var (empresaId, usuarioId) = Context();
        ValidateAmount(request.Cantidad, request.Motivo);
        _ = await Product(request.ProductoId, ct);
        _ = await Warehouse(request.AlmacenId, ct);

        for (var attempt = 1; attempt <= MaxConcurrencyAttempts; attempt++)
        {
            await using var transaction = await repository.BeginTransactionAsync(ct);
            try
            {
                var inventory = await repository.GetInventarioSnapshotAsync(request.AlmacenId, request.ProductoId, ct);
                var before = inventory?.Cantidad ?? 0;
                if (!incoming && before < request.Cantidad) throw new BusinessRuleException("Stock insuficiente.");
                var after = incoming ? before + request.Cantidad : before - request.Cantidad;

                if (inventory is null)
                {
                    if (!incoming) throw new BusinessRuleException("Stock insuficiente.");
                    repository.Add(Inventario.Create(empresaId, request.AlmacenId, request.ProductoId, after));
                }
                else if (!await repository.TrySetQuantityAsync(inventory.Id, inventory.Version, after, ct))
                {
                    await transaction.RollbackAsync(ct); repository.ClearTracking(); continue;
                }

                repository.Add(MovimientoInventario.Create(empresaId, request.AlmacenId, request.ProductoId, usuarioId,
                    incoming ? TipoMovimiento.Entrada : TipoMovimiento.Salida, request.Cantidad, before, after, request.Motivo, request.Referencia));
                await repository.SaveChangesAsync(ct);
                await transaction.CommitAsync(ct);
                repository.ClearTracking();
                return await GetInventarioAsync(request.AlmacenId, request.ProductoId, ct);
            }
            catch
            {
                await transaction.RollbackAsync(ct);
                repository.ClearTracking();
                throw;
            }
        }
        throw new ConflictException("No fue posible actualizar el stock debido a operaciones concurrentes. Intente nuevamente.");
    }

    private async Task<Categoria> Category(Guid id, CancellationToken ct) =>
        await repository.GetCategoriaAsync(id, ct) ?? throw new NotFoundException("Categoría no encontrada.");
    private async Task<Producto> Product(Guid id, CancellationToken ct) =>
        await repository.GetProductoAsync(id, ct) ?? throw new NotFoundException("Producto no encontrado.");
    private async Task<Almacen> Warehouse(Guid id, CancellationToken ct) =>
        await repository.GetAlmacenAsync(id, ct) ?? throw new NotFoundException("Almacén no encontrado.");
    private Guid Tenant() => currentUser.EmpresaId ?? throw new AuthenticationException("No existe un tenant válido.");
    private (Guid EmpresaId, Guid UsuarioId) Context() => (Tenant(), currentUser.UsuarioId ?? throw new AuthenticationException("No existe un usuario válido."));
    private static string Normalize(string value) => string.IsNullOrWhiteSpace(value) ? throw new ValidationException("El valor es obligatorio.") : value.Trim().ToUpperInvariant();
    private static void ValidateAmount(decimal amount, string reason)
    {
        if (amount <= 0) throw new ValidationException("La cantidad debe ser mayor que cero.");
        if (string.IsNullOrWhiteSpace(reason)) throw new ValidationException("El motivo es obligatorio.");
    }

    private static CategoriaResponse Map(Categoria x) => new(x.Id, x.Nombre, x.Descripcion, x.Activo);
    private static ProductoResponse Map(Producto x) => new(x.Id, x.CategoriaId, x.Categoria.Nombre, x.Codigo, x.Nombre, x.Descripcion,
        x.UnidadMedida, x.PrecioCompra, x.PrecioVenta, x.StockMinimo, x.AfectoIgv, x.Activo);
    private static AlmacenResponse Map(Almacen x) => new(x.Id, x.Codigo, x.Nombre, x.Direccion, x.Activo);
    private static InventarioResponse Map(Inventario x) => new(x.Id, x.AlmacenId, x.Almacen.Nombre, x.ProductoId, x.Producto.Codigo,
        x.Producto.Nombre, x.Cantidad, x.Producto.StockMinimo, x.Cantidad <= x.Producto.StockMinimo, x.Version, x.ActualizadoEn);
    private static MovimientoResponse Map(MovimientoInventario x) => new(x.Id, x.AlmacenId, x.ProductoId, x.UsuarioId, x.TipoMovimiento,
        x.Cantidad, x.StockAnterior, x.StockPosterior, x.Motivo, x.Referencia, x.TransferenciaId, x.CreadoEn);
}
