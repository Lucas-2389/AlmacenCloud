using AlmacenCloud.Domain.Enums;

namespace AlmacenCloud.Domain.Entities;

public sealed class CompraDetalle
{
    private CompraDetalle() { }

    private CompraDetalle(Guid compraId, Guid empresaId, Guid productoId, string codigo, string nombre, UnidadMedida unidad,
        decimal cantidad, decimal precioUnitario, decimal subtotal, decimal igv, decimal total)
    {
        Id = Guid.NewGuid(); CompraId = compraId; EmpresaId = empresaId; ProductoId = productoId;
        CodigoProducto = codigo; NombreProducto = nombre; UnidadMedida = unidad; Cantidad = cantidad;
        PrecioUnitario = precioUnitario; Subtotal = subtotal; Igv = igv; Total = total;
    }

    public Guid Id { get; private set; }
    public Guid EmpresaId { get; private set; }
    public Guid CompraId { get; private set; }
    public Guid ProductoId { get; private set; }
    public string CodigoProducto { get; private set; } = null!;
    public string NombreProducto { get; private set; } = null!;
    public UnidadMedida UnidadMedida { get; private set; }
    public decimal Cantidad { get; private set; }
    public decimal PrecioUnitario { get; private set; }
    public decimal Subtotal { get; private set; }
    public decimal Igv { get; private set; }
    public decimal Total { get; private set; }
    public Compra Compra { get; private set; } = null!;
    public Producto Producto { get; private set; } = null!;

    public static CompraDetalle Create(Guid compraId, Guid empresaId, Guid productoId, string codigo, string nombre, UnidadMedida unidad,
        decimal cantidad, decimal precioUnitario, decimal subtotal, decimal igv, decimal total) =>
        new(compraId, empresaId, productoId, codigo, nombre, unidad, cantidad, precioUnitario, subtotal, igv, total);
}
