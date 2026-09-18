namespace AlmacenCloud.Domain.Entities;

public sealed class UsuarioRol
{
    private UsuarioRol() { }

    private UsuarioRol(Guid usuarioId, Guid rolId)
    {
        UsuarioId = usuarioId;
        RolId = rolId;
    }

    public Guid UsuarioId { get; private set; }
    public Guid RolId { get; private set; }
    public Usuario Usuario { get; private set; } = null!;
    public Rol Rol { get; private set; } = null!;

    public static UsuarioRol Create(Guid usuarioId, Guid rolId) => new(usuarioId, rolId);
}
