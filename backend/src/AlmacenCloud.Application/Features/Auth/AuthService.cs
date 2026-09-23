using System.Net.Mail;
using System.Security.Cryptography;
using System.Text;
using AlmacenCloud.Application.Abstractions;
using AlmacenCloud.Application.Common;
using AlmacenCloud.Application.DTOs;
using AlmacenCloud.Domain.Common;
using AlmacenCloud.Domain.Entities;

namespace AlmacenCloud.Application.Features.Auth;

public sealed class AuthService(
    IIdentityRepository repository,
    IPasswordService passwordService,
    IJwtTokenGenerator jwtTokenGenerator,
    IPasswordResetNotifier passwordResetNotifier,
    ICurrentUser currentUser) : IAuthService
{
    public async Task<RegisterCompanyResponse> RegisterCompanyAsync(RegisterCompanyRequest request, CancellationToken cancellationToken)
    {
        ValidateRegistration(request);
        var ruc = request.Ruc.Trim();
        var email = request.Admin.Email.Trim().ToLowerInvariant();

        await using var transaction = await repository.BeginTransactionAsync(cancellationToken);
        try
        {
            if (await repository.RucExistsAsync(ruc, cancellationToken))
                throw new ConflictException("El RUC ya está registrado.");
            if (await repository.EmailExistsAsync(email, cancellationToken))
                throw new ConflictException("El email ya está registrado.");

            var empresa = Empresa.Create(ruc, request.RazonSocial, request.NombreComercial, request.Direccion, request.Telefono, request.Email);
            var usuario = Usuario.Create(empresa.Id, request.Admin.Nombre, request.Admin.Apellidos, email);
            usuario.SetPasswordHash(passwordService.Hash(usuario, request.Admin.Password));

            var rol = await repository.FindRoleAsync(RoleNames.AdminEmpresa, cancellationToken);
            var isNewRole = rol is null;
            rol ??= Rol.Create(RoleNames.AdminEmpresa);
            repository.Add(empresa);
            repository.Add(usuario);
            if (isNewRole)
                repository.Add(rol);
            repository.Add(UsuarioRol.Create(usuario.Id, rol.Id));

            await repository.SaveChangesAsync(cancellationToken);
            await transaction.CommitAsync(cancellationToken);
            return new RegisterCompanyResponse(empresa.Id, usuario.Id);
        }
        catch
        {
            await transaction.RollbackAsync(cancellationToken);
            throw;
        }
    }

    public async Task<AuthResponse> LoginAsync(LoginRequest request, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.Email) || string.IsNullOrWhiteSpace(request.Password))
            throw new AuthenticationException("Credenciales inválidas.");

        var usuario = await repository.FindUserForLoginAsync(request.Email.Trim().ToLowerInvariant(), cancellationToken);
        if (usuario is null || !usuario.Activo || !usuario.Empresa.Activo || !passwordService.Verify(usuario, request.Password))
            throw new AuthenticationException("Credenciales inválidas.");

        var roles = usuario.UsuarioRoles.Select(x => x.Rol.Nombre).OrderBy(x => x).ToArray();
        var token = jwtTokenGenerator.Generate(usuario, roles);
        return new AuthResponse(token.AccessToken, token.ExpiresIn, new UserResponse(usuario.Id, usuario.Nombre, usuario.Email, usuario.EmpresaId, roles));
    }

    public async Task<CurrentUserResponse> GetCurrentUserAsync(CancellationToken cancellationToken)
    {
        if (!currentUser.IsAuthenticated || currentUser.UsuarioId is not Guid usuarioId || currentUser.EmpresaId is not Guid empresaId)
            throw new AuthenticationException("El contexto autenticado no es válido.");

        var usuario = await repository.FindCurrentUserAsync(usuarioId, empresaId, cancellationToken)
            ?? throw new AuthenticationException("Usuario no encontrado.");
        var roles = usuario.UsuarioRoles.Select(x => x.Rol.Nombre).OrderBy(x => x).ToArray();
        return new CurrentUserResponse(usuario.Id, usuario.Nombre, usuario.Email, usuario.EmpresaId, usuario.Empresa.RazonSocial, roles);
    }

    public async Task RequestPasswordResetAsync(ForgotPasswordRequest request, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.Email)) return;
        var email = request.Email.Trim().ToLowerInvariant();
        var usuario = await repository.FindUserForPasswordResetAsync(email, cancellationToken);
        if (usuario is null || !usuario.Activo || !usuario.Empresa.Activo) return;

        var now = DateTime.UtcNow;
        var rawToken = ToBase64Url(RandomNumberGenerator.GetBytes(32));
        await repository.InvalidatePasswordResetTokensAsync(usuario.Id, now, cancellationToken);
        repository.Add(PasswordResetToken.Create(usuario.Id, usuario.EmpresaId, HashToken(rawToken), now.AddMinutes(30)));
        await repository.SaveChangesAsync(cancellationToken);
        await passwordResetNotifier.SendAsync(usuario.Email, usuario.Nombre, rawToken, cancellationToken);
    }

    public async Task ResetPasswordAsync(ResetPasswordRequest request, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.Token) || string.IsNullOrWhiteSpace(request.NewPassword) || request.NewPassword.Length < 8)
            throw new ValidationException("El enlace no es válido o la nueva contraseña no cumple el mínimo de 8 caracteres.");

        var token = await repository.FindPasswordResetTokenAsync(HashToken(request.Token), cancellationToken);
        var now = DateTime.UtcNow;
        if (token is null || !token.IsValid(now) || !token.Usuario.Activo || !token.Usuario.Empresa.Activo)
            throw new ValidationException("El enlace de recuperación no es válido o ha vencido.");

        await using var transaction = await repository.BeginTransactionAsync(cancellationToken);
        try
        {
            token.Usuario.SetPasswordHash(passwordService.Hash(token.Usuario, request.NewPassword));
            token.MarkUsed(now);
            await repository.InvalidatePasswordResetTokensAsync(token.UsuarioId, now, cancellationToken);
            await repository.SaveChangesAsync(cancellationToken);
            await transaction.CommitAsync(cancellationToken);
        }
        catch
        {
            await transaction.RollbackAsync(cancellationToken);
            throw;
        }
    }

    private static string HashToken(string token) => Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(token)));
    private static string ToBase64Url(byte[] bytes) => Convert.ToBase64String(bytes).TrimEnd('=').Replace('+', '-').Replace('/', '_');

    private static void ValidateRegistration(RegisterCompanyRequest request)
    {
        if (request.Admin is null) throw new ValidationException("El administrador es obligatorio.");
        if (request.Ruc?.Length != 11 || !request.Ruc.All(char.IsDigit)) throw new ValidationException("El RUC debe contener 11 dígitos.");
        if (string.IsNullOrWhiteSpace(request.RazonSocial)) throw new ValidationException("La razón social es obligatoria.");
        if (string.IsNullOrWhiteSpace(request.Admin.Nombre) || string.IsNullOrWhiteSpace(request.Admin.Apellidos)) throw new ValidationException("Nombre y apellidos son obligatorios.");
        try { _ = new MailAddress(request.Admin.Email); } catch { throw new ValidationException("El email no es válido."); }
        if (request.Admin.Password?.Length < 8) throw new ValidationException("La contraseña debe tener al menos 8 caracteres.");
    }
}
