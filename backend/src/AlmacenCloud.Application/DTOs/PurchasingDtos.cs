using AlmacenCloud.Domain.Enums;

namespace AlmacenCloud.Application.DTOs;

public sealed record CreateCompraItemRequest(Guid ProductoId, decimal Cantidad, decimal PrecioUnitario);
public sealed record CreateCompraRequest(Guid ProveedorId, Guid AlmacenId, string? NumeroDocumentoProveedor,
    IReadOnlyCollection<CreateCompraItemRequest> Items, string? Observacion);
public sealed record CompraDetalleResponse(Guid Id, Guid ProductoId, string CodigoProducto, string NombreProducto,
    UnidadMedida UnidadMedida, decimal Cantidad, decimal PrecioUnitario, decimal Subtotal, decimal Igv, decimal Total);
public sealed record CompraResponse(Guid Id, string Numero, DateTime Fecha, EstadoCompra Estado,
    Guid ProveedorId, string Proveedor, Guid AlmacenId, string Almacen, Guid UsuarioId, string Usuario,
    string? NumeroDocumentoProveedor, decimal Subtotal, decimal Igv, decimal Total, string? Observacion,
    Guid? AnuladoPorUsuarioId, DateTime? AnuladoEn, IReadOnlyCollection<CompraDetalleResponse> Detalles);
public sealed record CompraListResponse(Guid Id, string Numero, DateTime Fecha, EstadoCompra Estado,
    string Proveedor, string Almacen, string? NumeroDocumentoProveedor, decimal Total);
