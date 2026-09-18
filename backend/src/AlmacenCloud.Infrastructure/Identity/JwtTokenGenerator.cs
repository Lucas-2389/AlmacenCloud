using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using AlmacenCloud.Application.Abstractions;
using AlmacenCloud.Domain.Entities;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;

namespace AlmacenCloud.Infrastructure.Identity;

public sealed class JwtTokenGenerator(IOptions<JwtSettings> options) : IJwtTokenGenerator
{
    public TokenResult Generate(Usuario usuario, IReadOnlyCollection<string> roles)
    {
        var settings = options.Value;
        if (Encoding.UTF8.GetByteCount(settings.Secret) < 32)
            throw new InvalidOperationException("Jwt:Secret debe contener al menos 32 bytes.");

        var now = DateTime.UtcNow;
        var expires = now.AddMinutes(settings.ExpirationMinutes);
        var claims = new List<Claim>
        {
            new(JwtRegisteredClaimNames.Sub, usuario.Id.ToString()),
            new(JwtRegisteredClaimNames.Email, usuario.Email),
            new("empresa_id", usuario.EmpresaId.ToString())
        };
        claims.AddRange(roles.Select(role => new Claim(ClaimTypes.Role, role)));

        var token = new JwtSecurityToken(
            settings.Issuer,
            settings.Audience,
            claims,
            now,
            expires,
            new SigningCredentials(new SymmetricSecurityKey(Encoding.UTF8.GetBytes(settings.Secret)), SecurityAlgorithms.HmacSha256));

        return new TokenResult(new JwtSecurityTokenHandler().WriteToken(token), (long)(expires - now).TotalSeconds);
    }
}
