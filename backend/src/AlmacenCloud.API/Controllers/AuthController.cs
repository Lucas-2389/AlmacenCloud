using AlmacenCloud.Application.DTOs;
using AlmacenCloud.Application.Features.Auth;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AlmacenCloud.API.Controllers;

[ApiController]
[Route("api/v1/auth")]
public sealed class AuthController(IAuthService authService) : ControllerBase
{
    [AllowAnonymous]
    [HttpPost("register-company")]
    [ProducesResponseType<RegisterCompanyResponse>(StatusCodes.Status201Created)]
    public async Task<IActionResult> RegisterCompany(RegisterCompanyRequest request, CancellationToken cancellationToken)
    {
        var response = await authService.RegisterCompanyAsync(request, cancellationToken);
        return StatusCode(StatusCodes.Status201Created, response);
    }

    [AllowAnonymous]
    [HttpPost("login")]
    [ProducesResponseType<AuthResponse>(StatusCodes.Status200OK)]
    public async Task<ActionResult<AuthResponse>> Login(LoginRequest request, CancellationToken cancellationToken) =>
        Ok(await authService.LoginAsync(request, cancellationToken));

    [Authorize]
    [HttpGet("me")]
    [ProducesResponseType<CurrentUserResponse>(StatusCodes.Status200OK)]
    public async Task<ActionResult<CurrentUserResponse>> Me(CancellationToken cancellationToken) =>
        Ok(await authService.GetCurrentUserAsync(cancellationToken));
}
