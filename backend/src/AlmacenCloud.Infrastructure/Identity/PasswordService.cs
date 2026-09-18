using AlmacenCloud.Application.Abstractions;
using AlmacenCloud.Domain.Entities;
using Microsoft.AspNetCore.Identity;

namespace AlmacenCloud.Infrastructure.Identity;

public sealed class PasswordService : IPasswordService
{
    private readonly PasswordHasher<Usuario> _hasher = new();

    public string Hash(Usuario usuario, string password) => _hasher.HashPassword(usuario, password);

    public bool Verify(Usuario usuario, string password) =>
        _hasher.VerifyHashedPassword(usuario, usuario.PasswordHash, password) != PasswordVerificationResult.Failed;
}
