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
[Tags("Autenticacion")]
public sealed class AutenticacionController(
    IniciarSesionCasoUso iniciarSesion,
    CambiarContrasenaCasoUso cambiarContrasena) : ControllerBase
{
    /// <summary>Mensaje único para todo rechazo, sin indicar qué dato es incorrecto (FR-003).</summary>
    public const string MensajeCredencialesInvalidas = "Usuario o contraseña incorrectos";

    [HttpPost("iniciar-sesion")]
    [AllowAnonymous]
    [EndpointSummary("Inicia sesión (FR-001, FR-003, FR-004)")]
    [EndpointDescription("Devuelve un JWT de 30 minutos. Ante credenciales inválidas, usuario inexistente, desactivado o bloqueado responde 401 con el mismo mensaje genérico.")]
    [ProducesResponseType<SesionRespuesta>(StatusCodes.Status200OK)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest, "application/problem+json")]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status401Unauthorized, "application/problem+json")]
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
    [EndpointSummary("Cambia la contraseña propia (FR-006)")]
    [EndpointDescription("Permitido aunque el cambio sea obligatorio. Entrega una sesión nueva sin el cambio pendiente.")]
    [ProducesResponseType<SesionRespuesta>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status401Unauthorized, "application/problem+json")]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status422UnprocessableEntity, "application/problem+json")]
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
    [EndpointSummary("Cierra la sesión")]
    [EndpointDescription("El cliente descarta el token; la API no guarda estado de sesión (DA07).")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status401Unauthorized, "application/problem+json")]
    public IActionResult CerrarSesion() => NoContent();

    private Guid UsuarioActualId() => Guid.Parse(User.FindFirst("sub")!.Value);   // del token JWT, no del cuerpo
}
