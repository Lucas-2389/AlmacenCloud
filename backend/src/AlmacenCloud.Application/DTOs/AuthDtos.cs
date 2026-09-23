namespace AlmacenCloud.Application.DTOs;

public sealed record RegisterCompanyRequest(
    string Ruc,
    string RazonSocial,
    string? NombreComercial,
    string? Direccion,
    string? Telefono,
    string? Email,
    RegisterAdminRequest Admin);

public sealed record RegisterAdminRequest(string Nombre, string Apellidos, string Email, string Password);
public sealed record LoginRequest(string Email, string Password);
public sealed record ForgotPasswordRequest(string Email);
public sealed record ResetPasswordRequest(string Token, string NewPassword);
public sealed record MessageResponse(string Message);
public sealed record UserResponse(Guid Id, string Nombre, string Email, Guid EmpresaId, IReadOnlyCollection<string> Roles);
public sealed record AuthResponse(string AccessToken, long ExpiresIn, UserResponse User);
public sealed record RegisterCompanyResponse(Guid EmpresaId, Guid UsuarioId);
public sealed record CurrentUserResponse(Guid UsuarioId, string Nombre, string Email, Guid EmpresaId, string RazonSocial, IReadOnlyCollection<string> Roles);
