using AlmacenCloud.Application.Abstractions;
using AlmacenCloud.Domain.Entities;
using AlmacenCloud.Domain.Enums;
using AlmacenCloud.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;

namespace AlmacenCloud.Infrastructure.Repositories;

public sealed class InventoryCoreRepository(AlmacenCloudDbContext dbContext) : IInventoryCoreRepository
{
    public async Task<IReadOnlyCollection<Categoria>> GetCategoriasAsync(CancellationToken ct) =>
        await dbContext.Categorias.AsNoTracking().Where(x => x.Activo).OrderBy(x => x.Nombre).ToArrayAsync(ct);
    public Task<Categoria?> GetCategoriaAsync(Guid id, CancellationToken ct) =>
        dbContext.Categorias.SingleOrDefaultAsync(x => x.Id == id && x.Activo, ct);
    public Task<bool> CategoriaNameExistsAsync(string name, Guid? excludingId, CancellationToken ct) =>
        dbContext.Categorias.AnyAsync(x => x.Activo && x.NombreActivoClave == name && (!excludingId.HasValue || x.Id != excludingId), ct);
    public void Add(Categoria entity) => dbContext.Categorias.Add(entity);

    public async Task<IReadOnlyCollection<Producto>> GetProductosAsync(string? search, CancellationToken ct)
    {
        var query = dbContext.Productos.AsNoTracking().Include(x => x.Categoria).Where(x => x.Activo);
        if (!string.IsNullOrWhiteSpace(search))
        {
            var value = search.Trim();
            query = query.Where(x => x.Codigo.Contains(value) || x.Nombre.Contains(value));
        }
        return await query.OrderBy(x => x.Nombre).ToArrayAsync(ct);
    }
    public Task<Producto?> GetProductoAsync(Guid id, CancellationToken ct) =>
        dbContext.Productos.Include(x => x.Categoria).SingleOrDefaultAsync(x => x.Id == id && x.Activo, ct);
    public Task<bool> ProductoCodeExistsAsync(string code, Guid? excludingId, CancellationToken ct) =>
        dbContext.Productos.AnyAsync(x => x.Codigo == code && (!excludingId.HasValue || x.Id != excludingId), ct);
    public void Add(Producto entity) => dbContext.Productos.Add(entity);

    public async Task<IReadOnlyCollection<Almacen>> GetAlmacenesAsync(CancellationToken ct) =>
        await dbContext.Almacenes.AsNoTracking().Where(x => x.Activo).OrderBy(x => x.Nombre).ToArrayAsync(ct);
    public Task<Almacen?> GetAlmacenAsync(Guid id, CancellationToken ct) =>
        dbContext.Almacenes.SingleOrDefaultAsync(x => x.Id == id && x.Activo, ct);
    public Task<bool> AlmacenCodeExistsAsync(string code, Guid? excludingId, CancellationToken ct) =>
        dbContext.Almacenes.AnyAsync(x => x.Codigo == code && (!excludingId.HasValue || x.Id != excludingId), ct);
    public void Add(Almacen entity) => dbContext.Almacenes.Add(entity);

    public async Task<IReadOnlyCollection<Inventario>> GetInventariosAsync(Guid? almacenId, Guid? productoId, bool? stockBajo, CancellationToken ct)
    {
        var query = dbContext.Inventarios.AsNoTracking().Include(x => x.Almacen).Include(x => x.Producto).AsQueryable();
        if (almacenId.HasValue) query = query.Where(x => x.AlmacenId == almacenId);
        if (productoId.HasValue) query = query.Where(x => x.ProductoId == productoId);
        if (stockBajo == true) query = query.Where(x => x.Cantidad <= x.Producto.StockMinimo);
        return await query.OrderBy(x => x.Almacen.Nombre).ThenBy(x => x.Producto.Nombre).ToArrayAsync(ct);
    }
    public Task<Inventario?> GetInventarioAsync(Guid almacenId, Guid productoId, CancellationToken ct) =>
        dbContext.Inventarios.AsNoTracking().Include(x => x.Almacen).Include(x => x.Producto)
            .SingleOrDefaultAsync(x => x.AlmacenId == almacenId && x.ProductoId == productoId, ct);
    public Task<Inventario?> GetInventarioSnapshotAsync(Guid almacenId, Guid productoId, CancellationToken ct) =>
        dbContext.Inventarios.AsNoTracking().SingleOrDefaultAsync(x => x.AlmacenId == almacenId && x.ProductoId == productoId, ct);

    public async Task<bool> TrySetQuantityAsync(Guid inventarioId, long expectedVersion, decimal newQuantity, CancellationToken ct)
    {
        if (newQuantity < 0) return false;
        var now = DateTime.UtcNow;
        if (dbContext.Database.IsRelational())
        {
            var affected = await dbContext.Inventarios
                .Where(x => x.Id == inventarioId && x.Version == expectedVersion && x.Cantidad >= 0)
                .ExecuteUpdateAsync(update => update
                    .SetProperty(x => x.Cantidad, newQuantity)
                    .SetProperty(x => x.Version, expectedVersion + 1)
                    .SetProperty(x => x.ActualizadoEn, now), ct);
            return affected == 1;
        }

        var entity = await dbContext.Inventarios.SingleOrDefaultAsync(x => x.Id == inventarioId, ct);
        if (entity is null || entity.Version != expectedVersion) return false;
        entity.ApplyQuantity(newQuantity, expectedVersion);
        try
        {
            await dbContext.SaveChangesAsync(ct);
            return true;
        }
        catch (DbUpdateConcurrencyException)
        {
            dbContext.ChangeTracker.Clear();
            return false;
        }
    }

    public void Add(Inventario entity) => dbContext.Inventarios.Add(entity);
    public void Add(MovimientoInventario entity) => dbContext.MovimientosInventario.Add(entity);

    public async Task<(IReadOnlyCollection<MovimientoInventario> Items, int Total)> GetMovimientosAsync(Guid? productoId, Guid? almacenId,
        TipoMovimiento? tipo, DateTime? desde, DateTime? hasta, int page, int pageSize, CancellationToken ct)
    {
        var query = dbContext.MovimientosInventario.AsNoTracking().AsQueryable();
        if (productoId.HasValue) query = query.Where(x => x.ProductoId == productoId);
        if (almacenId.HasValue) query = query.Where(x => x.AlmacenId == almacenId);
        if (tipo.HasValue) query = query.Where(x => x.TipoMovimiento == tipo);
        if (desde.HasValue) query = query.Where(x => x.CreadoEn >= desde.Value);
        if (hasta.HasValue) query = query.Where(x => x.CreadoEn <= hasta.Value);
        var total = await query.CountAsync(ct);
        var items = await query.OrderByDescending(x => x.CreadoEn).Skip((page - 1) * pageSize).Take(pageSize).ToArrayAsync(ct);
        return (items, total);
    }

    public Task<int> SaveChangesAsync(CancellationToken ct) => dbContext.SaveChangesAsync(ct);
    public void ClearTracking() => dbContext.ChangeTracker.Clear();

    public async Task<IAppTransaction> BeginTransactionAsync(CancellationToken ct)
    {
        if (!dbContext.Database.IsRelational()) return new NoopTransaction();
        return new EfTransaction(await dbContext.Database.BeginTransactionAsync(ct));
    }

    private sealed class EfTransaction(IDbContextTransaction transaction) : IAppTransaction
    {
        public Task CommitAsync(CancellationToken ct) => transaction.CommitAsync(ct);
        public Task RollbackAsync(CancellationToken ct) => transaction.RollbackAsync(ct);
        public ValueTask DisposeAsync() => transaction.DisposeAsync();
    }
    private sealed class NoopTransaction : IAppTransaction
    {
        public Task CommitAsync(CancellationToken ct) => Task.CompletedTask;
        public Task RollbackAsync(CancellationToken ct) => Task.CompletedTask;
        public ValueTask DisposeAsync() => ValueTask.CompletedTask;
    }
}
