using AlmacenCloud.Domain.Entities;
using AlmacenCloud.Domain.Enums;

namespace AlmacenCloud.Application.Abstractions;

public interface IInventoryCoreRepository
{
    Task<IReadOnlyCollection<Categoria>> GetCategoriasAsync(CancellationToken cancellationToken);
    Task<Categoria?> GetCategoriaAsync(Guid id, CancellationToken cancellationToken);
    Task<bool> CategoriaNameExistsAsync(string normalizedName, Guid? excludingId, CancellationToken cancellationToken);
    void Add(Categoria entity);

    Task<IReadOnlyCollection<Producto>> GetProductosAsync(string? search, CancellationToken cancellationToken);
    Task<Producto?> GetProductoAsync(Guid id, CancellationToken cancellationToken);
    Task<bool> ProductoCodeExistsAsync(string code, Guid? excludingId, CancellationToken cancellationToken);
    void Add(Producto entity);

    Task<IReadOnlyCollection<Almacen>> GetAlmacenesAsync(CancellationToken cancellationToken);
    Task<Almacen?> GetAlmacenAsync(Guid id, CancellationToken cancellationToken);
    Task<bool> AlmacenCodeExistsAsync(string code, Guid? excludingId, CancellationToken cancellationToken);
    void Add(Almacen entity);

    Task<IReadOnlyCollection<Inventario>> GetInventariosAsync(Guid? almacenId, Guid? productoId, bool? stockBajo, CancellationToken cancellationToken);
    Task<Inventario?> GetInventarioAsync(Guid almacenId, Guid productoId, CancellationToken cancellationToken);
    Task<Inventario?> GetInventarioSnapshotAsync(Guid almacenId, Guid productoId, CancellationToken cancellationToken);
    Task<bool> TrySetQuantityAsync(Guid inventarioId, long expectedVersion, decimal newQuantity, CancellationToken cancellationToken);
    void Add(Inventario entity);
    void Add(MovimientoInventario entity);
    Task<(IReadOnlyCollection<MovimientoInventario> Items, int Total)> GetMovimientosAsync(Guid? productoId, Guid? almacenId,
        TipoMovimiento? tipo, DateTime? desde, DateTime? hasta, int page, int pageSize, CancellationToken cancellationToken);

    Task<int> SaveChangesAsync(CancellationToken cancellationToken);
    Task<IAppTransaction> BeginTransactionAsync(CancellationToken cancellationToken);
    void ClearTracking();
}
