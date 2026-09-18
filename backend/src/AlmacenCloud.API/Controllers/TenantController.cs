using AlmacenCloud.Application.Abstractions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AlmacenCloud.API.Controllers;

[ApiController]
[Authorize]
[Route("api/v1/tenant")]
public sealed class TenantController(ICurrentUser currentUser) : ControllerBase
{
    [HttpGet("context")]
    public IActionResult Context()
    {
        if (currentUser.UsuarioId is not Guid usuarioId || currentUser.EmpresaId is not Guid empresaId)
            return Unauthorized();

        return Ok(new { usuarioId, empresaId, roles = currentUser.Roles });
    }
}
