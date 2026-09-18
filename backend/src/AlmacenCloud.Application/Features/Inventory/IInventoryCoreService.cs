using AlmacenCloud.Application.DTOs;
using AlmacenCloud.Domain.Enums;

namespace AlmacenCloud.Application.Features.Inventory;

public interface IInventoryCoreService
{
    Task<IReadOnlyCollection<CategoriaResponse>> GetCategoriasAsync(CancellationToken ct);
    Task<CategoriaResponse> GetCategoriaAsync(Guid id, CancellationToken ct);
    Task<CategoriaResponse> CreateCategoriaAsync(CategoriaRequest request, CancellationToken ct);
    Task<CategoriaResponse> UpdateCategoriaAsync(Guid id, CategoriaRequest request, CancellationToken ct);
    Task DeleteCategoriaAsync(Guid id, CancellationToken ct);

    Task<IReadOnlyCollection<ProductoResponse>> GetProductosAsync(string? search, CancellationToken ct);
    Task<ProductoResponse> GetProductoAsync(Guid id, CancellationToken ct);
    Task<ProductoResponse> CreateProductoAsync(ProductoRequest request, CancellationToken ct);
    Task<ProductoResponse> UpdateProductoAsync(Guid id, ProductoRequest request, CancellationToken ct);
    Task DeleteProductoAsync(Guid id, CancellationToken ct);

    Task<IReadOnlyCollection<AlmacenResponse>> GetAlmacenesAsync(CancellationToken ct);
    Task<AlmacenResponse> GetAlmacenAsync(Guid id, CancellationToken ct);
    Task<AlmacenResponse> CreateAlmacenAsync(AlmacenRequest request, CancellationToken ct);
    Task<AlmacenResponse> UpdateAlmacenAsync(Guid id, AlmacenRequest request, CancellationToken ct);
    Task DeleteAlmacenAsync(Guid id, CancellationToken ct);

    Task<IReadOnlyCollection<InventarioResponse>> GetInventariosAsync(Guid? almacenId, Guid? productoId, bool? stockBajo, CancellationToken ct);
    Task<InventarioResponse> GetInventarioAsync(Guid almacenId, Guid productoId, CancellationToken ct);
    Task<InventarioResponse> EntradaAsync(MovimientoStockRequest request, CancellationToken ct);
    Task<InventarioResponse> SalidaAsync(MovimientoStockRequest request, CancellationToken ct);
    Task TransferenciaAsync(TransferenciaRequest request, CancellationToken ct);
    Task<PagedResponse<MovimientoResponse>> GetMovimientosAsync(Guid? productoId, Guid? almacenId, TipoMovimiento? tipo,
        DateTime? desde, DateTime? hasta, int page, int pageSize, CancellationToken ct);
}
