using AlmacenCloud.Domain.Enums;

namespace AlmacenCloud.Domain.Entities;

public sealed class MovimientoInventario
{
    private MovimientoInventario() { }

    private MovimientoInventario(Guid empresaId, Guid almacenId, Guid productoId, Guid usuarioId,
        TipoMovimiento tipo, decimal cantidad, decimal stockAnterior, decimal stockPosterior,
        string motivo, string? referencia, Guid? transferenciaId, Guid? ventaId)
    {
        Id = Guid.NewGuid();
        EmpresaId = empresaId;
        AlmacenId = almacenId;
        ProductoId = productoId;
        UsuarioId = usuarioId;
        TipoMovimiento = tipo;
        Cantidad = cantidad;
        StockAnterior = stockAnterior;
        StockPosterior = stockPosterior;
        Motivo = motivo.Trim();
        Referencia = string.IsNullOrWhiteSpace(referencia) ? null : referencia.Trim();
        TransferenciaId = transferenciaId;
        VentaId = ventaId;
        CreadoEn = DateTime.UtcNow;
    }

    public Guid Id { get; private set; }
    public Guid EmpresaId { get; private set; }
    public Guid AlmacenId { get; private set; }
    public Guid ProductoId { get; private set; }
    public Guid UsuarioId { get; private set; }
    public TipoMovimiento TipoMovimiento { get; private set; }
    public decimal Cantidad { get; private set; }
    public decimal StockAnterior { get; private set; }
    public decimal StockPosterior { get; private set; }
    public string Motivo { get; private set; } = null!;
    public string? Referencia { get; private set; }
    public Guid? TransferenciaId { get; private set; }
    public Guid? VentaId { get; private set; }
    public DateTime CreadoEn { get; private set; }
    public Almacen Almacen { get; private set; } = null!;
    public Producto Producto { get; private set; } = null!;
    public Usuario Usuario { get; private set; } = null!;
    public Venta? Venta { get; private set; }

    public static MovimientoInventario Create(Guid empresaId, Guid almacenId, Guid productoId, Guid usuarioId,
        TipoMovimiento tipo, decimal cantidad, decimal stockAnterior, decimal stockPosterior,
        string motivo, string? referencia = null, Guid? transferenciaId = null, Guid? ventaId = null)
    {
        if (cantidad <= 0) throw new ArgumentOutOfRangeException(nameof(cantidad));
        if (string.IsNullOrWhiteSpace(motivo)) throw new ArgumentException("El motivo es obligatorio.", nameof(motivo));
        return new(empresaId, almacenId, productoId, usuarioId, tipo, cantidad, stockAnterior, stockPosterior, motivo, referencia, transferenciaId, ventaId);
    }
}
