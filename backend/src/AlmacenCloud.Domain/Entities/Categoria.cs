namespace AlmacenCloud.Domain.Entities;

public sealed class Categoria
{
    private Categoria() { }

    private Categoria(Guid empresaId, string nombre, string? descripcion)
    {
        Id = Guid.NewGuid();
        EmpresaId = empresaId;
        Nombre = Required(nombre, nameof(nombre));
        NombreActivoClave = Normalize(Nombre);
        Descripcion = Clean(descripcion);
        Activo = true;
        CreadoEn = DateTime.UtcNow;
        ActualizadoEn = CreadoEn;
    }

    public Guid Id { get; private set; }
    public Guid EmpresaId { get; private set; }
    public string Nombre { get; private set; } = null!;
    public string? NombreActivoClave { get; private set; }
    public string? Descripcion { get; private set; }
    public bool Activo { get; private set; }
    public DateTime CreadoEn { get; private set; }
    public DateTime ActualizadoEn { get; private set; }

    public static Categoria Create(Guid empresaId, string nombre, string? descripcion) => new(empresaId, nombre, descripcion);

    public void Update(string nombre, string? descripcion)
    {
        Nombre = Required(nombre, nameof(nombre));
        NombreActivoClave = Normalize(Nombre);
        Descripcion = Clean(descripcion);
        ActualizadoEn = DateTime.UtcNow;
    }

    public void Deactivate()
    {
        Activo = false;
        NombreActivoClave = null;
        ActualizadoEn = DateTime.UtcNow;
    }

    private static string Required(string value, string name) => string.IsNullOrWhiteSpace(value) ? throw new ArgumentException("El valor es obligatorio.", name) : value.Trim();
    private static string Normalize(string value) => value.Trim().ToUpperInvariant();
    private static string? Clean(string? value) => string.IsNullOrWhiteSpace(value) ? null : value.Trim();
}
