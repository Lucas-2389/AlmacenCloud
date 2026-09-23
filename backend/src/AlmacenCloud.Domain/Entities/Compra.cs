using AlmacenCloud.Domain.Enums;

namespace AlmacenCloud.Domain.Entities;

public sealed class Compra
{
    private Compra() { }

    private Compra(Guid empresaId, Guid proveedorId, Guid almacenId, Guid usuarioId, string numero,
        decimal subtotal, decimal igv, decimal total, string? numeroDocumentoProveedor, string? observacion)
    {
        Id = Guid.NewGuid();
        EmpresaId = empresaId;
        ProveedorId = proveedorId;
        AlmacenId = almacenId;
        UsuarioId = usuarioId;
        Numero = numero;
        Fecha = DateTime.UtcNow;
        Subtotal = subtotal;
        Igv = igv;
        Total = total;
        Estado = EstadoCompra.Registrada;
        NumeroDocumentoProveedor = Clean(numeroDocumentoProveedor);
        Observacion = Clean(observacion);
        CreadoEn = Fecha;
        Version = 1;
    }

    public Guid Id { get; private set; }
    public Guid EmpresaId { get; private set; }
    public Guid ProveedorId { get; private set; }
    public Guid AlmacenId { get; private set; }
    public Guid UsuarioId { get; private set; }
    public string Numero { get; private set; } = null!;
    public DateTime Fecha { get; private set; }
    public decimal Subtotal { get; private set; }
    public decimal Igv { get; private set; }
    public decimal Total { get; private set; }
    public EstadoCompra Estado { get; private set; }
    public string? NumeroDocumentoProveedor { get; private set; }
    public string? Observacion { get; private set; }
    public DateTime CreadoEn { get; private set; }
    public DateTime? AnuladoEn { get; private set; }
    public Guid? AnuladoPorUsuarioId { get; private set; }
    public long Version { get; private set; }
    public Proveedor Proveedor { get; private set; } = null!;
    public Almacen Almacen { get; private set; } = null!;
    public Usuario Usuario { get; private set; } = null!;
    public ICollection<CompraDetalle> Detalles { get; private set; } = [];

    public static Compra Create(Guid empresaId, Guid proveedorId, Guid almacenId, Guid usuarioId, string numero,
        decimal subtotal, decimal igv, decimal total, string? numeroDocumentoProveedor, string? observacion) =>
        new(empresaId, proveedorId, almacenId, usuarioId, numero, subtotal, igv, total, numeroDocumentoProveedor, observacion);

    public void AddDetail(CompraDetalle detail) => Detalles.Add(detail);

    public void MarkAnnulled(Guid usuarioId, long expectedVersion)
    {
        Estado = EstadoCompra.Anulada;
        AnuladoPorUsuarioId = usuarioId;
        AnuladoEn = DateTime.UtcNow;
        Version = expectedVersion + 1;
    }

    private static string? Clean(string? value) => string.IsNullOrWhiteSpace(value) ? null : value.Trim();
}
