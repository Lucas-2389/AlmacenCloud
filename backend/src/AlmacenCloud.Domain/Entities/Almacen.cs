namespace AlmacenCloud.Domain.Entities;

public sealed class Almacen
{
    private Almacen() { }

    private Almacen(Guid empresaId, string codigo, string nombre, string? direccion)
    {
        Id = Guid.NewGuid();
        EmpresaId = empresaId;
        CreadoEn = DateTime.UtcNow;
        ActualizadoEn = CreadoEn;
        Activo = true;
        Apply(codigo, nombre, direccion);
    }

    public Guid Id { get; private set; }
    public Guid EmpresaId { get; private set; }
    public string Codigo { get; private set; } = null!;
    public string Nombre { get; private set; } = null!;
    public string? Direccion { get; private set; }
    public bool Activo { get; private set; }
    public DateTime CreadoEn { get; private set; }
    public DateTime ActualizadoEn { get; private set; }

    public static Almacen Create(Guid empresaId, string codigo, string nombre, string? direccion) => new(empresaId, codigo, nombre, direccion);
    public void Update(string codigo, string nombre, string? direccion) { Apply(codigo, nombre, direccion); ActualizadoEn = DateTime.UtcNow; }
    public void Deactivate() { Activo = false; ActualizadoEn = DateTime.UtcNow; }

    private void Apply(string codigo, string nombre, string? direccion)
    {
        if (string.IsNullOrWhiteSpace(codigo)) throw new ArgumentException("El código es obligatorio.", nameof(codigo));
        if (string.IsNullOrWhiteSpace(nombre)) throw new ArgumentException("El nombre es obligatorio.", nameof(nombre));
        Codigo = codigo.Trim().ToUpperInvariant();
        Nombre = nombre.Trim();
        Direccion = string.IsNullOrWhiteSpace(direccion) ? null : direccion.Trim();
    }
}
