using Microsoft.AspNetCore.Mvc;

namespace Sigh.Api.Tests.Soporte;

/// <summary>
/// Endpoint que solo existe en las pruebas. No lleva [Authorize]: lo protege la política de respaldo
/// (FR-007) y el requisito de cambio de contraseña pendiente (FR-006).
/// </summary>
[ApiController]
[Route("api/v1/pruebas/protegido")]
public sealed class ControladorProtegidoPrueba : ControllerBase
{
    [HttpGet]
    public IActionResult Obtener() => Ok(new { mensaje = "acceso concedido" });
}
