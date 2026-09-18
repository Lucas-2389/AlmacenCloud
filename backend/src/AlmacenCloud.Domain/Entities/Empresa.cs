namespace AlmacenCloud.Domain.Entities;

public sealed class Empresa
{
    private Empresa() { }

    private Empresa(string ruc, string razonSocial, string? nombreComercial, string? direccion, string? telefono, string? email)
    {
        if (ruc.Length != 11 || !ruc.All(char.IsDigit))
            throw new ArgumentException("El RUC debe contener exactamente 11 dígitos.", nameof(ruc));

        Id = Guid.NewGuid();
        Ruc = ruc;
        RazonSocial = razonSocial;
        NombreComercial = nombreComercial;
        Direccion = direccion;
        Telefono = telefono;
        Email = email;
        Activo = true;
        CreadoEn = DateTime.UtcNow;
        ActualizadoEn = CreadoEn;
    }

    public Guid Id { get; private set; }
    public string Ruc { get; private set; } = null!;
    public string RazonSocial { get; private set; } = null!;
    public string? NombreComercial { get; private set; }
    public string? Direccion { get; private set; }
    public string? Telefono { get; private set; }
    public string? Email { get; private set; }
    public bool Activo { get; private set; }
    public DateTime CreadoEn { get; private set; }
    public DateTime ActualizadoEn { get; private set; }
    public ICollection<Usuario> Usuarios { get; private set; } = [];

    public static Empresa Create(string ruc, string razonSocial, string? nombreComercial, string? direccion, string? telefono, string? email) =>
        new(ruc.Trim(), razonSocial.Trim(), Clean(nombreComercial), Clean(direccion), Clean(telefono), Clean(email));

    private static string? Clean(string? value) => string.IsNullOrWhiteSpace(value) ? null : value.Trim();
}
