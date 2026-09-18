namespace AlmacenCloud.Domain.Entities;

public sealed class Usuario
{
    private Usuario() { }

    private Usuario(Guid empresaId, string nombre, string apellidos, string email)
    {
        Id = Guid.NewGuid();
        EmpresaId = empresaId;
        Nombre = nombre.Trim();
        Apellidos = apellidos.Trim();
        Email = email.Trim().ToLowerInvariant();
        Activo = true;
        CreadoEn = DateTime.UtcNow;
        ActualizadoEn = CreadoEn;
    }

    public Guid Id { get; private set; }
    public Guid EmpresaId { get; private set; }
    public string Nombre { get; private set; } = null!;
    public string Apellidos { get; private set; } = null!;
    public string Email { get; private set; } = null!;
    public string PasswordHash { get; private set; } = null!;
    public bool Activo { get; private set; }
    public DateTime CreadoEn { get; private set; }
    public DateTime ActualizadoEn { get; private set; }
    public Empresa Empresa { get; private set; } = null!;
    public ICollection<UsuarioRol> UsuarioRoles { get; private set; } = [];

    public static Usuario Create(Guid empresaId, string nombre, string apellidos, string email) =>
        new(empresaId, nombre, apellidos, email);

    public void SetPasswordHash(string passwordHash) => PasswordHash = passwordHash;
}
