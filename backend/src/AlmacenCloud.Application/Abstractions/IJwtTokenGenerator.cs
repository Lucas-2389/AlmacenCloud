using AlmacenCloud.Domain.Entities;

namespace AlmacenCloud.Application.Abstractions;

public interface IJwtTokenGenerator
{
    TokenResult Generate(Usuario usuario, IReadOnlyCollection<string> roles);
}

public sealed record TokenResult(string AccessToken, long ExpiresIn);
