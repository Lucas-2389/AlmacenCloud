using AlmacenCloud.Domain.Enums;

namespace AlmacenCloud.Application.DTOs;

public sealed record ClienteRequest(TipoDocumento TipoDocumento, string NumeroDocumento, string NombreRazonSocial,
    string? Direccion, string? Telefono, string? Email);
public sealed record ClienteResponse(Guid Id, TipoDocumento TipoDocumento, string NumeroDocumento, string NombreRazonSocial,
    string? Direccion, string? Telefono, string? Email, bool Activo);
public sealed record ProveedorRequest(string Ruc, string RazonSocial, string? NombreComercial, string? Direccion, string? Telefono, string? Email);
public sealed record ProveedorResponse(Guid Id, string Ruc, string RazonSocial, string? NombreComercial, string? Direccion, string? Telefono, string? Email, bool Activo);

public sealed record CreateVentaItemRequest(Guid ProductoId, decimal Cantidad, decimal PrecioUnitario);
public sealed record CreateVentaRequest(Guid? ClienteId, Guid AlmacenId, IReadOnlyCollection<CreateVentaItemRequest> Items, string? Observacion);
public sealed record VentaDetalleResponse(Guid Id, Guid ProductoId, string CodigoProducto, string NombreProducto, UnidadMedida UnidadMedida,
    decimal Cantidad, decimal PrecioUnitario, decimal Subtotal, decimal Igv, decimal Total);
public sealed record VentaResponse(Guid Id, string Numero, DateTime Fecha, EstadoVenta Estado, Guid? ClienteId, string Cliente,
    Guid AlmacenId, string Almacen, Guid UsuarioId, string Usuario, decimal Subtotal, decimal Igv, decimal Total,
    string? Observacion, Guid? AnuladaPorUsuarioId, DateTime? AnuladaEn, IReadOnlyCollection<VentaDetalleResponse> Detalles);
public sealed record VentaListResponse(Guid Id, string Numero, DateTime Fecha, EstadoVenta Estado, string Cliente, string Almacen, decimal Total);
