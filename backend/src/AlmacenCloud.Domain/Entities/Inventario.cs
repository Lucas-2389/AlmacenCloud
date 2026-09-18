namespace AlmacenCloud.Domain.Entities;

public sealed class Inventario
{
    private Inventario() { }

    private Inventario(Guid empresaId, Guid almacenId, Guid productoId, decimal cantidad)
    {
        if (cantidad < 0) throw new ArgumentOutOfRangeException(nameof(cantidad));
        Id = Guid.NewGuid();
        EmpresaId = empresaId;
        AlmacenId = almacenId;
        ProductoId = productoId;
        Cantidad = cantidad;
        Version = 1;
        ActualizadoEn = DateTime.UtcNow;
    }

    public Guid Id { get; private set; }
    public Guid EmpresaId { get; private set; }
    public Guid AlmacenId { get; private set; }
    public Guid ProductoId { get; private set; }
    public decimal Cantidad { get; private set; }
    public long Version { get; private set; }
    public DateTime ActualizadoEn { get; private set; }
    public Almacen Almacen { get; private set; } = null!;
    public Producto Producto { get; private set; } = null!;

    public static Inventario Create(Guid empresaId, Guid almacenId, Guid productoId, decimal cantidad = 0) => new(empresaId, almacenId, productoId, cantidad);

    public void ApplyQuantity(decimal newQuantity, long expectedVersion)
    {
        if (newQuantity < 0) throw new InvalidOperationException("El stock no puede ser negativo.");
        Cantidad = newQuantity;
        Version = expectedVersion + 1;
        ActualizadoEn = DateTime.UtcNow;
    }
}
