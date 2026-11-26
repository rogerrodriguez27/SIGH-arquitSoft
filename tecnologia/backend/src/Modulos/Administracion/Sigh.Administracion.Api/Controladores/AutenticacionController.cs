using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Sigh.Administracion.Api.Respuestas;
using Sigh.Administracion.Api.Solicitudes;
using Sigh.Administracion.Aplicacion.Autenticacion.CambiarContrasena;
using Sigh.Administracion.Aplicacion.Autenticacion.IniciarSesion;
using Sigh.Compartido.Autorizacion;

namespace Sigh.Administracion.Api.Controladores;

/// <summary>Inicio de sesión, cambio de contraseña y cierre de sesión (RF21, contrato api-administracion).</summary>
[ApiController]
[Route("api/v1/autenticacion")]
public sealed class AutenticacionController(
    IniciarSesionCasoUso iniciarSesion,
    CambiarContrasenaCasoUso cambiarContrasena) : ControllerBase
{
    /// <summary>Mensaje único para todo rechazo, sin indicar qué dato es incorrecto (FR-003).</summary>
    public const string MensajeCredencialesInvalidas = "Usuario o contraseña incorrectos";

    [HttpPost("iniciar-sesion")]
    [AllowAnonymous]
    public async Task<ActionResult<SesionRespuesta>> IniciarSesion(IniciarSesionSolicitud solicitud, CancellationToken ct)
    {
        var sesion = await iniciarSesion.EjecutarAsync(
            new IniciarSesionComando(solicitud.NombreUsuario, solicitud.Contrasena), ct);

        if (sesion is null)
            return Problem(statusCode: StatusCodes.Status401Unauthorized, title: MensajeCredencialesInvalidas);

        return Ok(SesionRespuesta.Desde(sesion));
    }

    [HttpPost("cambiar-contrasena")]
    [Authorize]
    [PermitirConCambioContrasenaPendiente]
    public async Task<ActionResult<SesionRespuesta>> CambiarContrasena(CambiarContrasenaSolicitud solicitud,
        CancellationToken ct)
    {
        var sesion = await cambiarContrasena.EjecutarAsync(
            new CambiarContrasenaComando(UsuarioActualId(), solicitud.ContrasenaActual, solicitud.ContrasenaNueva), ct);
        return Ok(SesionRespuesta.Desde(sesion));
    }

    /// <summary>La API no guarda sesiones (DA07): el cliente descarta el token.</summary>
    [HttpPost("cerrar-sesion")]
    [Authorize]
    [PermitirConCambioContrasenaPendiente]
    public IActionResult CerrarSesion() => NoContent();

    private Guid UsuarioActualId() => Guid.Parse(User.FindFirst("sub")!.Value);   // del token JWT, no del cuerpo
}
