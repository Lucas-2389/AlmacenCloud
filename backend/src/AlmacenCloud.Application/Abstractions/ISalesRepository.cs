using AlmacenCloud.Domain.Entities;
using AlmacenCloud.Domain.Enums;

namespace AlmacenCloud.Application.Abstractions;

public interface ISalesRepository
{
    Task<IReadOnlyCollection<Cliente>> GetClientesAsync(string? search, CancellationToken ct);
    Task<Cliente?> GetClienteAsync(Guid id, CancellationToken ct);
    Task<bool> ClienteDocumentExistsAsync(TipoDocumento type, string document, Guid? excludingId, CancellationToken ct);
    void Add(Cliente entity);
    Task<IReadOnlyCollection<Proveedor>> GetProveedoresAsync(string? search, CancellationToken ct);
    Task<Proveedor?> GetProveedorAsync(Guid id, CancellationToken ct);
    Task<bool> ProveedorRucExistsAsync(string ruc, Guid? excludingId, CancellationToken ct);
    void Add(Proveedor entity);

    Task<SecuenciaVenta?> GetSequenceSnapshotAsync(CancellationToken ct);
    Task<bool> TryAdvanceSequenceAsync(long expectedVersion, CancellationToken ct);
    void Add(SecuenciaVenta entity);
    void Add(Venta entity);
    Task<Venta?> GetVentaAsync(Guid id, CancellationToken ct);
    Task<Venta?> GetVentaSnapshotAsync(Guid id, CancellationToken ct);
    Task<bool> TryMarkSaleAnnulledAsync(Guid id, long expectedVersion, Guid userId, CancellationToken ct);
    Task<(IReadOnlyCollection<Venta> Items, int Total)> GetVentasAsync(DateTime? desde, DateTime? hasta, Guid? clienteId,
        Guid? almacenId, int page, int pageSize, CancellationToken ct);

    Task<int> SaveChangesAsync(CancellationToken ct);
    Task<IAppTransaction> BeginTransactionAsync(CancellationToken ct);
    void ClearTracking();
    bool IsTransient(Exception exception);
}
