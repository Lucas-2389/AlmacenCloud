namespace AlmacenCloud.Application.Abstractions;

public interface ICurrentUser
{
    bool IsAuthenticated { get; }
    Guid? UsuarioId { get; }
    Guid? EmpresaId { get; }
    string? Email { get; }
    IReadOnlyCollection<string> Roles { get; }
}
