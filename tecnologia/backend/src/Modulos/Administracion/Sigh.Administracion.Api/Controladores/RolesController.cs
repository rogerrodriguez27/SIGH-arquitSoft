using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Sigh.Administracion.Aplicacion.Permisos.ConsultarMatrizPermisos;
using Sigh.Administracion.Dominio.Permisos;

namespace Sigh.Administracion.Api.Controladores;

/// <summary>Consulta de la matriz fija de roles y permisos (FR-013). Es de solo lectura.</summary>
[ApiController]
[Route("api/v1/roles")]
[Authorize(Policy = CodigoPermiso.GestionarUsuarios)]
public sealed class RolesController(ConsultarMatrizPermisosCasoUso consultarMatriz) : ControllerBase
{
    [HttpGet("matriz-permisos")]
    public ActionResult<MatrizPermisosDto> ConsultarMatriz() => Ok(consultarMatriz.Ejecutar());
}
