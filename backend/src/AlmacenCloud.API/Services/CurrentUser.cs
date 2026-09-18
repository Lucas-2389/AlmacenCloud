using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using AlmacenCloud.Application.Abstractions;

namespace AlmacenCloud.API.Services;

public sealed class CurrentUser(IHttpContextAccessor accessor) : ICurrentUser, ITenantContext
{
    private ClaimsPrincipal? Principal => accessor.HttpContext?.User;
    public bool IsAuthenticated => Principal?.Identity?.IsAuthenticated == true;
    public Guid? UsuarioId => ReadGuid(JwtRegisteredClaimNames.Sub) ?? ReadGuid(ClaimTypes.NameIdentifier);
    public Guid? EmpresaId => ReadGuid("empresa_id");
    public bool HasTenant => IsAuthenticated && EmpresaId.HasValue;
    public string? Email => Principal?.FindFirstValue(JwtRegisteredClaimNames.Email) ?? Principal?.FindFirstValue(ClaimTypes.Email);
    public IReadOnlyCollection<string> Roles => Principal?.FindAll(ClaimTypes.Role).Select(x => x.Value).Distinct().ToArray() ?? [];

    private Guid? ReadGuid(string claimType) =>
        Guid.TryParse(Principal?.FindFirstValue(claimType), out var value) ? value : null;
}
