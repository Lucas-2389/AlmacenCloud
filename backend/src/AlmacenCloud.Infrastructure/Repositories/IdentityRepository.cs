using AlmacenCloud.Application.Abstractions;
using AlmacenCloud.Domain.Entities;
using AlmacenCloud.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;

namespace AlmacenCloud.Infrastructure.Repositories;

public sealed class IdentityRepository(AlmacenCloudDbContext dbContext) : IIdentityRepository
{
    public Task<bool> RucExistsAsync(string ruc, CancellationToken cancellationToken) =>
        dbContext.Empresas.IgnoreQueryFilters().AnyAsync(x => x.Ruc == ruc, cancellationToken);

    public Task<bool> EmailExistsAsync(string email, CancellationToken cancellationToken) =>
        dbContext.Usuarios.IgnoreQueryFilters().AnyAsync(x => x.Email == email, cancellationToken);

    public Task<Rol?> FindRoleAsync(string name, CancellationToken cancellationToken) =>
        dbContext.Roles.SingleOrDefaultAsync(x => x.Nombre == name, cancellationToken);

    public Task<Usuario?> FindUserForLoginAsync(string email, CancellationToken cancellationToken) =>
        dbContext.Usuarios.IgnoreQueryFilters()
            .Include(x => x.Empresa)
            .Include(x => x.UsuarioRoles).ThenInclude(x => x.Rol)
            .SingleOrDefaultAsync(x => x.Email == email, cancellationToken);

    public Task<Usuario?> FindCurrentUserAsync(Guid usuarioId, Guid empresaId, CancellationToken cancellationToken) =>
        dbContext.Usuarios
            .Include(x => x.Empresa)
            .Include(x => x.UsuarioRoles).ThenInclude(x => x.Rol)
            .SingleOrDefaultAsync(x => x.Id == usuarioId && x.EmpresaId == empresaId, cancellationToken);

    public Task<Usuario?> FindUserForPasswordResetAsync(string email, CancellationToken cancellationToken) =>
        dbContext.Usuarios.IgnoreQueryFilters().Include(x => x.Empresa)
            .SingleOrDefaultAsync(x => x.Email == email, cancellationToken);

    public Task<PasswordResetToken?> FindPasswordResetTokenAsync(string tokenHash, CancellationToken cancellationToken) =>
        dbContext.PasswordResetTokens.IgnoreQueryFilters()
            .Include(x => x.Usuario).ThenInclude(x => x.Empresa)
            .SingleOrDefaultAsync(x => x.TokenHash == tokenHash, cancellationToken);

    public async Task InvalidatePasswordResetTokensAsync(Guid usuarioId, DateTime now, CancellationToken cancellationToken)
    {
        var tokens = await dbContext.PasswordResetTokens.IgnoreQueryFilters()
            .Where(x => x.UsuarioId == usuarioId && x.UsadoEn == null)
            .ToListAsync(cancellationToken);
        foreach (var token in tokens) token.MarkUsed(now);
    }

    public void Add(Empresa empresa) => dbContext.Empresas.Add(empresa);
    public void Add(Usuario usuario) => dbContext.Usuarios.Add(usuario);
    public void Add(Rol rol) => dbContext.Roles.Add(rol);
    public void Add(UsuarioRol usuarioRol) => dbContext.UsuarioRoles.Add(usuarioRol);
    public void Add(PasswordResetToken token) => dbContext.PasswordResetTokens.Add(token);
    public Task<int> SaveChangesAsync(CancellationToken cancellationToken) => dbContext.SaveChangesAsync(cancellationToken);

    public async Task<IAppTransaction> BeginTransactionAsync(CancellationToken cancellationToken)
    {
        if (!dbContext.Database.IsRelational())
            return new NoopAppTransaction();
        return new EfAppTransaction(await dbContext.Database.BeginTransactionAsync(cancellationToken));
    }

    private sealed class EfAppTransaction(IDbContextTransaction transaction) : IAppTransaction
    {
        public Task CommitAsync(CancellationToken cancellationToken) => transaction.CommitAsync(cancellationToken);
        public Task RollbackAsync(CancellationToken cancellationToken) => transaction.RollbackAsync(cancellationToken);
        public ValueTask DisposeAsync() => transaction.DisposeAsync();
    }

    private sealed class NoopAppTransaction : IAppTransaction
    {
        public Task CommitAsync(CancellationToken cancellationToken) => Task.CompletedTask;
        public Task RollbackAsync(CancellationToken cancellationToken) => Task.CompletedTask;
        public ValueTask DisposeAsync() => ValueTask.CompletedTask;
    }
}
