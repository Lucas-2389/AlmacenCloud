using AlmacenCloud.Application.DTOs;
using AlmacenCloud.Domain.Enums;

namespace AlmacenCloud.Application.Features.Purchasing;

public interface IPurchasingService
{
    Task<CompraResponse> CreateCompraAsync(CreateCompraRequest request, CancellationToken ct);
    Task<PagedResponse<CompraListResponse>> GetComprasAsync(DateTime? desde, DateTime? hasta, Guid? proveedorId,
        Guid? almacenId, EstadoCompra? estado, int page, int pageSize, CancellationToken ct);
    Task<CompraResponse> GetCompraAsync(Guid id, CancellationToken ct);
    Task<CompraResponse> AnnulCompraAsync(Guid id, CancellationToken ct);
}
