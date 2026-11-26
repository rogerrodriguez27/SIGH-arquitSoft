using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Sigh.Administracion.Aplicacion.Permisos.ConsultarMatrizPermisos;
using Sigh.Administracion.Dominio.Permisos;

namespace Sigh.Administracion.Api.Controladores;

/// <summary>Consulta de la matriz fija de roles y permisos (FR-013). Es de solo lectura.</summary>
[ApiController]
[Route("api/v1/roles")]
[Tags("Permisos")]
[Authorize(Policy = CodigoPermiso.GestionarUsuarios)]
public sealed class RolesController(ConsultarMatrizPermisosCasoUso consultarMatriz) : ControllerBase
{
    [HttpGet("matriz-permisos")]
    [EndpointSummary("Consulta la matriz fija de roles y permisos (FR-013) – permiso usuarios.gestionar")]
    [EndpointDescription("Los roles y sus permisos son fijos: no existen operaciones para crearlos ni editarlos.")]
    [ProducesResponseType<MatrizPermisosDto>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status401Unauthorized, "application/problem+json")]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status403Forbidden, "application/problem+json")]
    public ActionResult<MatrizPermisosDto> ConsultarMatriz() => Ok(consultarMatriz.Ejecutar());
}
