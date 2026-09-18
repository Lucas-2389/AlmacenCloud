using AlmacenCloud.Domain.Enums;

namespace AlmacenCloud.Application.DTOs;

public sealed record CategoriaRequest(string Nombre, string? Descripcion);
public sealed record CategoriaResponse(Guid Id, string Nombre, string? Descripcion, bool Activo);

public sealed record ProductoRequest(Guid CategoriaId, string Codigo, string Nombre, string? Descripcion,
    UnidadMedida UnidadMedida, decimal PrecioCompra, decimal PrecioVenta, decimal StockMinimo, bool AfectoIgv);
public sealed record ProductoResponse(Guid Id, Guid CategoriaId, string Categoria, string Codigo, string Nombre,
    string? Descripcion, UnidadMedida UnidadMedida, decimal PrecioCompra, decimal PrecioVenta,
    decimal StockMinimo, bool AfectoIgv, bool Activo);

public sealed record AlmacenRequest(string Codigo, string Nombre, string? Direccion);
public sealed record AlmacenResponse(Guid Id, string Codigo, string Nombre, string? Direccion, bool Activo);

public sealed record MovimientoStockRequest(Guid AlmacenId, Guid ProductoId, decimal Cantidad, string Motivo, string? Referencia);
public sealed record TransferenciaRequest(Guid ProductoId, Guid AlmacenOrigenId, Guid AlmacenDestinoId, decimal Cantidad, string Motivo);
public sealed record InventarioResponse(Guid Id, Guid AlmacenId, string Almacen, Guid ProductoId, string CodigoProducto,
    string Producto, decimal Cantidad, decimal StockMinimo, bool StockBajo, long Version, DateTime ActualizadoEn);
public sealed record MovimientoResponse(Guid Id, Guid AlmacenId, Guid ProductoId, Guid UsuarioId, TipoMovimiento Tipo,
    decimal Cantidad, decimal StockAnterior, decimal StockPosterior, string Motivo, string? Referencia,
    Guid? TransferenciaId, Guid? VentaId, DateTime CreadoEn);
public sealed record PagedResponse<T>(IReadOnlyCollection<T> Items, int Page, int PageSize, int Total);
