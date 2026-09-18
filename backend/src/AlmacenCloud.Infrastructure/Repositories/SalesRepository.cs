using AlmacenCloud.Application.Abstractions;
using AlmacenCloud.Domain.Entities;
using AlmacenCloud.Domain.Enums;
using AlmacenCloud.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using MySql.Data.MySqlClient;

namespace AlmacenCloud.Infrastructure.Repositories;

public sealed class SalesRepository(AlmacenCloudDbContext dbContext) : ISalesRepository
{
    public async Task<IReadOnlyCollection<Cliente>> GetClientesAsync(string? search, CancellationToken ct)
    {
        var query = dbContext.Clientes.AsNoTracking().Where(x => x.Activo);
        if (!string.IsNullOrWhiteSpace(search)) { var term = search.Trim(); query = query.Where(x => x.NumeroDocumento.Contains(term) || x.NombreRazonSocial.Contains(term)); }
        return await query.OrderBy(x => x.NombreRazonSocial).ToArrayAsync(ct);
    }
    public Task<Cliente?> GetClienteAsync(Guid id, CancellationToken ct) => dbContext.Clientes.SingleOrDefaultAsync(x => x.Id == id && x.Activo, ct);
    public Task<bool> ClienteDocumentExistsAsync(TipoDocumento type, string document, Guid? excludingId, CancellationToken ct) =>
        dbContext.Clientes.AnyAsync(x => x.TipoDocumento == type && x.NumeroDocumento == document && (!excludingId.HasValue || x.Id != excludingId), ct);
    public void Add(Cliente entity) => dbContext.Clientes.Add(entity);

    public async Task<IReadOnlyCollection<Proveedor>> GetProveedoresAsync(string? search, CancellationToken ct)
    {
        var query = dbContext.Proveedores.AsNoTracking().Where(x => x.Activo);
        if (!string.IsNullOrWhiteSpace(search)) { var term = search.Trim(); query = query.Where(x => x.Ruc.Contains(term) || x.RazonSocial.Contains(term)); }
        return await query.OrderBy(x => x.RazonSocial).ToArrayAsync(ct);
    }
    public Task<Proveedor?> GetProveedorAsync(Guid id, CancellationToken ct) => dbContext.Proveedores.SingleOrDefaultAsync(x => x.Id == id && x.Activo, ct);
    public Task<bool> ProveedorRucExistsAsync(string ruc, Guid? excludingId, CancellationToken ct) =>
        dbContext.Proveedores.AnyAsync(x => x.Ruc == ruc && (!excludingId.HasValue || x.Id != excludingId), ct);
    public void Add(Proveedor entity) => dbContext.Proveedores.Add(entity);

    public Task<SecuenciaVenta?> GetSequenceSnapshotAsync(CancellationToken ct) => dbContext.SecuenciasVenta.AsNoTracking().SingleOrDefaultAsync(ct);
    public async Task<bool> TryAdvanceSequenceAsync(long expectedVersion, CancellationToken ct)
    {
        if (dbContext.Database.IsRelational())
            return await dbContext.SecuenciasVenta.Where(x => x.Version == expectedVersion)
                .ExecuteUpdateAsync(x => x.SetProperty(s => s.UltimoNumero, s => s.UltimoNumero + 1).SetProperty(s => s.Version, expectedVersion + 1), ct) == 1;
        var sequence = await dbContext.SecuenciasVenta.SingleOrDefaultAsync(ct);
        if (sequence is null || sequence.Version != expectedVersion) return false;
        sequence.Advance(expectedVersion);
        try { await dbContext.SaveChangesAsync(ct); return true; }
        catch (DbUpdateConcurrencyException) { dbContext.ChangeTracker.Clear(); return false; }
    }
    public void Add(SecuenciaVenta entity) => dbContext.SecuenciasVenta.Add(entity);
    public void Add(Venta entity) => dbContext.Ventas.Add(entity);

    public Task<Venta?> GetVentaAsync(Guid id, CancellationToken ct) => dbContext.Ventas.AsNoTracking()
        .Include(x => x.Cliente).Include(x => x.Almacen).Include(x => x.Usuario).Include(x => x.Detalles)
        .SingleOrDefaultAsync(x => x.Id == id, ct);
    public Task<Venta?> GetVentaSnapshotAsync(Guid id, CancellationToken ct) => dbContext.Ventas.AsNoTracking().Include(x => x.Detalles).SingleOrDefaultAsync(x => x.Id == id, ct);
    public async Task<bool> TryMarkSaleAnnulledAsync(Guid id, long expectedVersion, Guid userId, CancellationToken ct)
    {
        var now = DateTime.UtcNow;
        if (dbContext.Database.IsRelational())
            return await dbContext.Ventas.Where(x => x.Id == id && x.Version == expectedVersion && x.Estado == EstadoVenta.Registrada)
                .ExecuteUpdateAsync(x => x.SetProperty(s => s.Estado, EstadoVenta.Anulada).SetProperty(s => s.AnuladaPorUsuarioId, userId)
                    .SetProperty(s => s.AnuladaEn, now).SetProperty(s => s.Version, expectedVersion + 1), ct) == 1;
        var sale = await dbContext.Ventas.SingleOrDefaultAsync(x => x.Id == id, ct);
        if (sale is null || sale.Version != expectedVersion || sale.Estado != EstadoVenta.Registrada) return false;
        sale.MarkAnnulled(userId, expectedVersion);
        try { await dbContext.SaveChangesAsync(ct); return true; }
        catch (DbUpdateConcurrencyException) { dbContext.ChangeTracker.Clear(); return false; }
    }

    public async Task<(IReadOnlyCollection<Venta> Items, int Total)> GetVentasAsync(DateTime? desde, DateTime? hasta, Guid? clienteId,
        Guid? almacenId, int page, int pageSize, CancellationToken ct)
    {
        var query = dbContext.Ventas.AsNoTracking().Include(x => x.Cliente).Include(x => x.Almacen).AsQueryable();
        if (desde.HasValue) query = query.Where(x => x.Fecha >= desde.Value);
        if (hasta.HasValue) query = query.Where(x => x.Fecha <= hasta.Value);
        if (clienteId.HasValue) query = query.Where(x => x.ClienteId == clienteId);
        if (almacenId.HasValue) query = query.Where(x => x.AlmacenId == almacenId);
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
