namespace AlmacenCloud.Domain.Entities;

public sealed class Rol
{
    private Rol() { }

    private Rol(string nombre)
    {
        Id = Guid.NewGuid();
        Nombre = nombre;
    }

    public Guid Id { get; private set; }
    public string Nombre { get; private set; } = null!;
    public ICollection<UsuarioRol> UsuarioRoles { get; private set; } = [];

    public static Rol Create(string nombre) => new(nombre.Trim().ToUpperInvariant());
}
