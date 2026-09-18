using AlmacenCloud.Application.DTOs;

namespace AlmacenCloud.Application.Features.Sales;

public interface ISalesService
{
    Task<IReadOnlyCollection<ClienteResponse>> GetClientesAsync(string? search, CancellationToken ct);
    Task<ClienteResponse> GetClienteAsync(Guid id, CancellationToken ct);
    Task<ClienteResponse> CreateClienteAsync(ClienteRequest request, CancellationToken ct);
    Task<ClienteResponse> UpdateClienteAsync(Guid id, ClienteRequest request, CancellationToken ct);
    Task DeleteClienteAsync(Guid id, CancellationToken ct);
    Task<IReadOnlyCollection<ProveedorResponse>> GetProveedoresAsync(string? search, CancellationToken ct);
    Task<ProveedorResponse> GetProveedorAsync(Guid id, CancellationToken ct);
    Task<ProveedorResponse> CreateProveedorAsync(ProveedorRequest request, CancellationToken ct);
    Task<ProveedorResponse> UpdateProveedorAsync(Guid id, ProveedorRequest request, CancellationToken ct);
    Task DeleteProveedorAsync(Guid id, CancellationToken ct);
    Task<VentaResponse> CreateVentaAsync(CreateVentaRequest request, CancellationToken ct);
    Task<PagedResponse<VentaListResponse>> GetVentasAsync(DateTime? desde, DateTime? hasta, Guid? clienteId, Guid? almacenId, int page, int pageSize, CancellationToken ct);
    Task<VentaResponse> GetVentaAsync(Guid id, CancellationToken ct);
    Task<VentaResponse> AnnulVentaAsync(Guid id, CancellationToken ct);
}
