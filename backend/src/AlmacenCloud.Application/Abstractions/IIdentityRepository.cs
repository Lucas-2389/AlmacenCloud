using AlmacenCloud.Domain.Entities;

namespace AlmacenCloud.Application.Abstractions;

public interface IIdentityRepository
{
    Task<bool> RucExistsAsync(string ruc, CancellationToken cancellationToken);
    Task<bool> EmailExistsAsync(string email, CancellationToken cancellationToken);
    Task<Rol?> FindRoleAsync(string name, CancellationToken cancellationToken);
    Task<Usuario?> FindUserForLoginAsync(string email, CancellationToken cancellationToken);
    Task<Usuario?> FindCurrentUserAsync(Guid usuarioId, Guid empresaId, CancellationToken cancellationToken);
    Task<Usuario?> FindUserForPasswordResetAsync(string email, CancellationToken cancellationToken);
    Task<PasswordResetToken?> FindPasswordResetTokenAsync(string tokenHash, CancellationToken cancellationToken);
    Task InvalidatePasswordResetTokensAsync(Guid usuarioId, DateTime now, CancellationToken cancellationToken);
    void Add(Empresa empresa);
    void Add(Usuario usuario);
    void Add(Rol rol);
    void Add(UsuarioRol usuarioRol);
    void Add(PasswordResetToken token);
    Task<int> SaveChangesAsync(CancellationToken cancellationToken);
    Task<IAppTransaction> BeginTransactionAsync(CancellationToken cancellationToken);
}

public interface IAppTransaction : IAsyncDisposable
{
    Task CommitAsync(CancellationToken cancellationToken);
    Task RollbackAsync(CancellationToken cancellationToken);
}
