using AlmacenCloud.Application.DTOs;
using AlmacenCloud.Application.Features.Auth;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.Extensions.Options;
using AlmacenCloud.Infrastructure.Identity;

namespace AlmacenCloud.API.Controllers;

[ApiController]
[Route("api/v1/auth")]
public sealed class AuthController(IAuthService authService, IOptions<PasswordResetSettings> passwordResetOptions) : ControllerBase
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

    [AllowAnonymous]
    [EnableRateLimiting("password-recovery")]
    [HttpPost("forgot-password")]
    [ProducesResponseType<MessageResponse>(StatusCodes.Status202Accepted)]
    public async Task<IActionResult> ForgotPassword(ForgotPasswordRequest request, CancellationToken cancellationToken)
    {
        if (!passwordResetOptions.Value.Enabled) return RecoveryUnavailable();
        await authService.RequestPasswordResetAsync(request, cancellationToken);
        return Accepted(new MessageResponse("Si el correo pertenece a una cuenta activa, recibirás instrucciones para restablecer la contraseña."));
    }

    [AllowAnonymous]
    [EnableRateLimiting("password-recovery")]
    [HttpPost("reset-password")]
    [ProducesResponseType<MessageResponse>(StatusCodes.Status200OK)]
    public async Task<IActionResult> ResetPassword(ResetPasswordRequest request, CancellationToken cancellationToken)
    {
        if (!passwordResetOptions.Value.Enabled) return RecoveryUnavailable();
        await authService.ResetPasswordAsync(request, cancellationToken);
        return Ok(new MessageResponse("La contraseña fue actualizada. Ya puedes iniciar sesión."));
    }

    [Authorize]
    [HttpGet("me")]
    [ProducesResponseType<CurrentUserResponse>(StatusCodes.Status200OK)]
    public async Task<ActionResult<CurrentUserResponse>> Me(CancellationToken cancellationToken) =>
        Ok(await authService.GetCurrentUserAsync(cancellationToken));

    private ObjectResult RecoveryUnavailable() => StatusCode(StatusCodes.Status503ServiceUnavailable, new ProblemDetails
    {
        Status = StatusCodes.Status503ServiceUnavailable,
        Title = "Servicio no disponible",
        Detail = "La recuperación de contraseña no está disponible temporalmente."
    });
}
