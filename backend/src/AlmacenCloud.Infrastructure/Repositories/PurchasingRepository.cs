using AlmacenCloud.Application.Abstractions;
using AlmacenCloud.Domain.Entities;
using AlmacenCloud.Domain.Enums;
using AlmacenCloud.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using MySql.Data.MySqlClient;

namespace AlmacenCloud.Infrastructure.Repositories;

public sealed class PurchasingRepository(AlmacenCloudDbContext dbContext) : IPurchasingRepository
{
    public Task<Proveedor?> GetProveedorAsync(Guid id, CancellationToken ct) =>
        dbContext.Proveedores.SingleOrDefaultAsync(x => x.Id == id && x.Activo, ct);

    public Task<SecuenciaCompra?> GetSequenceSnapshotAsync(CancellationToken ct) =>
        dbContext.SecuenciasCompra.AsNoTracking().SingleOrDefaultAsync(ct);

    public async Task<bool> TryAdvanceSequenceAsync(long expectedVersion, CancellationToken ct)
    {
        if (dbContext.Database.IsRelational())
            return await dbContext.SecuenciasCompra.Where(x => x.Version == expectedVersion)
                .ExecuteUpdateAsync(x => x.SetProperty(s => s.UltimoNumero, s => s.UltimoNumero + 1)
                    .SetProperty(s => s.Version, expectedVersion + 1), ct) == 1;
        var sequence = await dbContext.SecuenciasCompra.SingleOrDefaultAsync(ct);
        if (sequence is null || sequence.Version != expectedVersion) return false;
        sequence.Advance(expectedVersion);
        try { await dbContext.SaveChangesAsync(ct); return true; }
        catch (DbUpdateConcurrencyException) { dbContext.ChangeTracker.Clear(); return false; }
    }

    public void Add(SecuenciaCompra entity) => dbContext.SecuenciasCompra.Add(entity);
    public void Add(Compra entity) => dbContext.Compras.Add(entity);

    public Task<Compra?> GetCompraAsync(Guid id, CancellationToken ct) => dbContext.Compras.AsNoTracking()
        .Include(x => x.Proveedor).Include(x => x.Almacen).Include(x => x.Usuario).Include(x => x.Detalles)
        .SingleOrDefaultAsync(x => x.Id == id, ct);

    public Task<Compra?> GetCompraSnapshotAsync(Guid id, CancellationToken ct) => dbContext.Compras.AsNoTracking()
        .Include(x => x.Detalles).SingleOrDefaultAsync(x => x.Id == id, ct);

    public async Task<bool> TryMarkPurchaseAnnulledAsync(Guid id, long expectedVersion, Guid userId, CancellationToken ct)
    {
        var now = DateTime.UtcNow;
        if (dbContext.Database.IsRelational())
            return await dbContext.Compras.Where(x => x.Id == id && x.Version == expectedVersion && x.Estado == EstadoCompra.Registrada)
                .ExecuteUpdateAsync(x => x.SetProperty(p => p.Estado, EstadoCompra.Anulada)
                    .SetProperty(p => p.AnuladoPorUsuarioId, userId).SetProperty(p => p.AnuladoEn, now)
                    .SetProperty(p => p.Version, expectedVersion + 1), ct) == 1;
        var purchase = await dbContext.Compras.SingleOrDefaultAsync(x => x.Id == id, ct);
        if (purchase is null || purchase.Version != expectedVersion || purchase.Estado != EstadoCompra.Registrada) return false;
        purchase.MarkAnnulled(userId, expectedVersion);
        try { await dbContext.SaveChangesAsync(ct); return true; }
        catch (DbUpdateConcurrencyException) { dbContext.ChangeTracker.Clear(); return false; }
    }

    public async Task<(IReadOnlyCollection<Compra> Items, int Total)> GetComprasAsync(DateTime? desde, DateTime? hasta,
        Guid? proveedorId, Guid? almacenId, EstadoCompra? estado, int page, int pageSize, CancellationToken ct)
    {
        var query = dbContext.Compras.AsNoTracking().Include(x => x.Proveedor).Include(x => x.Almacen).AsQueryable();
        if (desde.HasValue) query = query.Where(x => x.Fecha >= desde.Value);
        if (hasta.HasValue) query = query.Where(x => x.Fecha <= hasta.Value);
        if (proveedorId.HasValue) query = query.Where(x => x.ProveedorId == proveedorId);
        if (almacenId.HasValue) query = query.Where(x => x.AlmacenId == almacenId);
        if (estado.HasValue) query = query.Where(x => x.Estado == estado.Value);
        var total = await query.CountAsync(ct);
        var items = await query.OrderByDescending(x => x.Fecha).Skip((page - 1) * pageSize).Take(pageSize).ToArrayAsync(ct);
        return (items, total);
    }

    public Task<int> SaveChangesAsync(CancellationToken ct) => dbContext.SaveChangesAsync(ct);
    public void ClearTracking() => dbContext.ChangeTracker.Clear();

    public bool IsTransient(Exception exception)
    {
        for (var current = exception; current is not null; current = current.InnerException!)
            if (current is MySqlException mysql && mysql.Number is 1205 or 1213 or 1062) return true;
        return false;
    }

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
