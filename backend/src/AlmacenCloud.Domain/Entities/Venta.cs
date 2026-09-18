using AlmacenCloud.Domain.Enums;

namespace AlmacenCloud.Domain.Entities;

public sealed class Venta
{
    private Venta() { }
    private Venta(Guid empresaId, Guid? clienteId, string clienteNombre, Guid almacenId, Guid usuarioId, string numero,
        decimal subtotal, decimal igv, decimal total, string? observacion)
    {
        Id = Guid.NewGuid(); EmpresaId = empresaId; ClienteId = clienteId; ClienteNombre = clienteNombre;
        AlmacenId = almacenId; UsuarioId = usuarioId; Numero = numero; Fecha = DateTime.UtcNow;
        Subtotal = subtotal; Igv = igv; Total = total; Estado = EstadoVenta.Registrada;
        Observacion = string.IsNullOrWhiteSpace(observacion) ? null : observacion.Trim(); CreadoEn = Fecha; Version = 1;
    }
    public Guid Id { get; private set; }
    public Guid EmpresaId { get; private set; }
    public Guid? ClienteId { get; private set; }
    public string ClienteNombre { get; private set; } = null!;
    public Guid AlmacenId { get; private set; }
    public Guid UsuarioId { get; private set; }
    public string Numero { get; private set; } = null!;
    public DateTime Fecha { get; private set; }
    public decimal Subtotal { get; private set; }
    public decimal Igv { get; private set; }
    public decimal Total { get; private set; }
    public EstadoVenta Estado { get; private set; }
    public string? Observacion { get; private set; }
    public DateTime CreadoEn { get; private set; }
    public Guid? AnuladaPorUsuarioId { get; private set; }
    public DateTime? AnuladaEn { get; private set; }
    public long Version { get; private set; }
    public Cliente? Cliente { get; private set; }
    public Almacen Almacen { get; private set; } = null!;
    public Usuario Usuario { get; private set; } = null!;
    public ICollection<VentaDetalle> Detalles { get; private set; } = [];

    public static Venta Create(Guid empresaId, Guid? clienteId, string clienteNombre, Guid almacenId, Guid usuarioId, string numero,
        decimal subtotal, decimal igv, decimal total, string? observacion) => new(empresaId, clienteId, clienteNombre, almacenId, usuarioId, numero, subtotal, igv, total, observacion);
    public void MarkAnnulled(Guid usuarioId, long expectedVersion)
    {
        Estado = EstadoVenta.Anulada; AnuladaPorUsuarioId = usuarioId; AnuladaEn = DateTime.UtcNow; Version = expectedVersion + 1;
    }
    public void AddDetail(VentaDetalle detail) => Detalles.Add(detail);
}
