using AlmacenCloud.Domain.Entities;
using AlmacenCloud.Domain.Enums;

namespace AlmacenCloud.Application.Abstractions;

public interface IPurchasingRepository
{
    Task<Proveedor?> GetProveedorAsync(Guid id, CancellationToken ct);
    Task<SecuenciaCompra?> GetSequenceSnapshotAsync(CancellationToken ct);
    Task<bool> TryAdvanceSequenceAsync(long expectedVersion, CancellationToken ct);
    void Add(SecuenciaCompra entity);
    void Add(Compra entity);
    Task<Compra?> GetCompraAsync(Guid id, CancellationToken ct);
    Task<Compra?> GetCompraSnapshotAsync(Guid id, CancellationToken ct);
    Task<bool> TryMarkPurchaseAnnulledAsync(Guid id, long expectedVersion, Guid userId, CancellationToken ct);
    Task<(IReadOnlyCollection<Compra> Items, int Total)> GetComprasAsync(DateTime? desde, DateTime? hasta,
        Guid? proveedorId, Guid? almacenId, EstadoCompra? estado, int page, int pageSize, CancellationToken ct);
    Task<int> SaveChangesAsync(CancellationToken ct);
    Task<IAppTransaction> BeginTransactionAsync(CancellationToken ct);
    void ClearTracking();
    bool IsTransient(Exception exception);
}
