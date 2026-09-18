using AlmacenCloud.Application.DTOs;

namespace AlmacenCloud.Application.Features.Auth;

public interface IAuthService
{
    Task<RegisterCompanyResponse> RegisterCompanyAsync(RegisterCompanyRequest request, CancellationToken cancellationToken);
    Task<AuthResponse> LoginAsync(LoginRequest request, CancellationToken cancellationToken);
    Task<CurrentUserResponse> GetCurrentUserAsync(CancellationToken cancellationToken);
}
