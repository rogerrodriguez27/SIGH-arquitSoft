using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Sigh.Administracion.Dominio.Permisos;

namespace Sigh.Api.Tests.Soporte;

/// <summary>
/// Solo existe en las pruebas: un endpoint por cada permiso fijo, protegido con su política, para
/// verificar la matriz completa rol × permiso (SC-001). Las funciones reales de los demás módulos se
/// protegerán con estas mismas políticas cuando se especifiquen.
/// </summary>
[ApiController]
[Route("api/v1/pruebas/permisos")]
public sealed class ControladorPermisosPrueba : ControllerBase
{
    private IActionResult Permitido() => Ok(new { permitido = true });

    [HttpGet(CodigoPermiso.GestionarUsuarios), Authorize(Policy = CodigoPermiso.GestionarUsuarios)]
    public IActionResult GestionarUsuarios() => Permitido();

    [HttpGet(CodigoPermiso.ConsultarBitacora), Authorize(Policy = CodigoPermiso.ConsultarBitacora)]
    public IActionResult ConsultarBitacora() => Permitido();

    [HttpGet(CodigoPermiso.RegistrarPacientes), Authorize(Policy = CodigoPermiso.RegistrarPacientes)]
    public IActionResult RegistrarPacientes() => Permitido();

    [HttpGet(CodigoPermiso.RegistrarTriaje), Authorize(Policy = CodigoPermiso.RegistrarTriaje)]
    public IActionResult RegistrarTriaje() => Permitido();

    [HttpGet(CodigoPermiso.ConsultarHce), Authorize(Policy = CodigoPermiso.ConsultarHce)]
    public IActionResult ConsultarHce() => Permitido();

    [HttpGet(CodigoPermiso.RegistrarAtencion), Authorize(Policy = CodigoPermiso.RegistrarAtencion)]
    public IActionResult RegistrarAtencion() => Permitido();

    [HttpGet(CodigoPermiso.RecetasExamenes), Authorize(Policy = CodigoPermiso.RecetasExamenes)]
    public IActionResult RecetasExamenes() => Permitido();

    [HttpGet(CodigoPermiso.DispensarRecetas), Authorize(Policy = CodigoPermiso.DispensarRecetas)]
    public IActionResult DispensarRecetas() => Permitido();

    [HttpGet(CodigoPermiso.GestionarInventario), Authorize(Policy = CodigoPermiso.GestionarInventario)]
    public IActionResult GestionarInventario() => Permitido();

    [HttpGet(CodigoPermiso.ResultadosLaboratorio), Authorize(Policy = CodigoPermiso.ResultadosLaboratorio)]
    public IActionResult ResultadosLaboratorio() => Permitido();

    [HttpGet(CodigoPermiso.ReportesEpidemiologicos), Authorize(Policy = CodigoPermiso.ReportesEpidemiologicos)]
    public IActionResult ReportesEpidemiologicos() => Permitido();

    [HttpGet(CodigoPermiso.DashboardsGestion), Authorize(Policy = CodigoPermiso.DashboardsGestion)]
    public IActionResult DashboardsGestion() => Permitido();

    [HttpGet(CodigoPermiso.RecibirNotificaciones), Authorize(Policy = CodigoPermiso.RecibirNotificaciones)]
    public IActionResult RecibirNotificaciones() => Permitido();
}
