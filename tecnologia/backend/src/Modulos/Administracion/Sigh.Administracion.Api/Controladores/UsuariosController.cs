using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Sigh.Administracion.Api.Respuestas;
using Sigh.Administracion.Api.Solicitudes;
using Sigh.Administracion.Aplicacion.Usuarios;
using Sigh.Administracion.Aplicacion.Usuarios.CambiarRoles;
using Sigh.Administracion.Aplicacion.Usuarios.ConsultarUsuario;
using Sigh.Administracion.Aplicacion.Usuarios.CrearUsuario;
using Sigh.Administracion.Aplicacion.Usuarios.DesactivarUsuario;
using Sigh.Administracion.Aplicacion.Usuarios.EditarUsuario;
using Sigh.Administracion.Aplicacion.Usuarios.ListarUsuarios;
using Sigh.Administracion.Aplicacion.Usuarios.ReactivarUsuario;
using Sigh.Administracion.Aplicacion.Usuarios.RestablecerContrasena;
using Sigh.Administracion.Dominio.Permisos;
using Sigh.Administracion.Dominio.Roles;
using Sigh.Administracion.Dominio.Usuarios;

namespace Sigh.Administracion.Api.Controladores;

/// <summary>
/// Gestión de usuarios y asignación de roles (HU14, RF22, contrato api-administracion). No existe
/// DELETE: los usuarios solo se desactivan (FR-009).
/// </summary>
[ApiController]
[Route("api/v1/usuarios")]
[Tags("Usuarios")]
[Authorize(Policy = CodigoPermiso.GestionarUsuarios)]
[ProducesResponseType<ProblemDetails>(StatusCodes.Status401Unauthorized, "application/problem+json")]
[ProducesResponseType<ProblemDetails>(StatusCodes.Status403Forbidden, "application/problem+json")]
public sealed class UsuariosController : ControllerBase
{
    [HttpGet]
    [EndpointSummary("Lista usuarios con búsqueda y filtros (FR-008) – permiso usuarios.gestionar")]
    [ProducesResponseType<PaginaRespuesta<UsuarioDto>>(StatusCodes.Status200OK)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest, "application/problem+json")]
    public async Task<ActionResult<PaginaRespuesta<UsuarioDto>>> Listar(
        [FromServices] ListarUsuariosCasoUso casoUso,
        [FromQuery, MaxLength(100)] string? busqueda,
        [FromQuery] CodigoRol? rol,
        [FromQuery] EstadoUsuario? estado,
        [FromQuery, Range(1, int.MaxValue)] int pagina = 1,
        [FromQuery, Range(1, 100)] int tamano = 20,
        CancellationToken ct = default)
    {
        var resultado = await casoUso.EjecutarAsync(new ListarUsuariosConsulta(busqueda, rol, estado, pagina, tamano), ct);
        return Ok(PaginaRespuesta<UsuarioDto>.Desde(resultado));
    }

    [HttpPost]
    [EndpointSummary("Registra un usuario (FR-008, FR-010) – permiso usuarios.gestionar")]
    [EndpointDescription("El usuario queda activo y deberá cambiar su contraseña al iniciar sesión. El nombre de usuario se normaliza (sin espacios en los extremos y en minúsculas).")]
    [ProducesResponseType<UsuarioDto>(StatusCodes.Status201Created)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest, "application/problem+json")]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict, "application/problem+json")]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status422UnprocessableEntity, "application/problem+json")]
    public async Task<ActionResult<UsuarioDto>> Crear([FromServices] CrearUsuarioCasoUso casoUso,
        CrearUsuarioSolicitud solicitud, CancellationToken ct)
    {
        var usuario = await casoUso.EjecutarAsync(new CrearUsuarioComando(ActorId(), solicitud.NombreUsuario,
            solicitud.NombreCompleto, solicitud.ContrasenaInicial, solicitud.Roles), ct);
        return Created($"/api/v1/usuarios/{usuario.Id}", usuario);
    }

    [HttpGet("{id:guid}")]
    [EndpointSummary("Consulta un usuario – permiso usuarios.gestionar")]
    [ProducesResponseType<UsuarioDto>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound, "application/problem+json")]
    public async Task<ActionResult<UsuarioDto>> Consultar([FromServices] ConsultarUsuarioCasoUso casoUso,
        Guid id, CancellationToken ct) =>
        Ok(await casoUso.EjecutarAsync(id, ct));

    [HttpPut("{id:guid}")]
    [EndpointSummary("Edita los datos básicos (nombre completo) – permiso usuarios.gestionar")]
    [ProducesResponseType<UsuarioDto>(StatusCodes.Status200OK)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest, "application/problem+json")]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound, "application/problem+json")]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status422UnprocessableEntity, "application/problem+json")]
    public async Task<ActionResult<UsuarioDto>> Editar([FromServices] EditarUsuarioCasoUso casoUso,
        Guid id, EditarUsuarioSolicitud solicitud, CancellationToken ct) =>
        Ok(await casoUso.EjecutarAsync(new EditarUsuarioComando(ActorId(), id, solicitud.NombreCompleto), ct));

    [HttpPut("{id:guid}/roles")]
    [EndpointSummary("Reemplaza los roles del usuario (FR-010, FR-014) – permiso usuarios.gestionar")]
    [EndpointDescription("Los nuevos roles aplican en la siguiente sesión del usuario. No se puede quitar el rol Administrador al último administrador activo ni a uno mismo.")]
    [ProducesResponseType<UsuarioDto>(StatusCodes.Status200OK)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest, "application/problem+json")]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound, "application/problem+json")]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status422UnprocessableEntity, "application/problem+json")]
    public async Task<ActionResult<UsuarioDto>> CambiarRoles([FromServices] CambiarRolesCasoUso casoUso,
        Guid id, CambiarRolesSolicitud solicitud, CancellationToken ct) =>
        Ok(await casoUso.EjecutarAsync(new CambiarRolesComando(ActorId(), id, solicitud.Roles), ct));

    [HttpPost("{id:guid}/desactivar")]
    [EndpointSummary("Desactiva al usuario (FR-008, FR-014) – permiso usuarios.gestionar")]
    [EndpointDescription("No existe la eliminación (FR-009). No se puede desactivar al último administrador activo ni a uno mismo.")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound, "application/problem+json")]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status422UnprocessableEntity, "application/problem+json")]
    public async Task<IActionResult> Desactivar([FromServices] DesactivarUsuarioCasoUso casoUso,
        Guid id, CancellationToken ct)
    {
        await casoUso.EjecutarAsync(new DesactivarUsuarioComando(ActorId(), id), ct);
        return NoContent();
    }

    [HttpPost("{id:guid}/reactivar")]
    [EndpointSummary("Reactiva al usuario – permiso usuarios.gestionar")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound, "application/problem+json")]
    public async Task<IActionResult> Reactivar([FromServices] ReactivarUsuarioCasoUso casoUso,
        Guid id, CancellationToken ct)
    {
        await casoUso.EjecutarAsync(new ReactivarUsuarioComando(ActorId(), id), ct);
        return NoContent();
    }

    [HttpPost("{id:guid}/restablecer-contrasena")]
    [EndpointSummary("Asigna una contraseña temporal y exige cambiarla (FR-015) – permiso usuarios.gestionar")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest, "application/problem+json")]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound, "application/problem+json")]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status422UnprocessableEntity, "application/problem+json")]
    public async Task<IActionResult> RestablecerContrasena([FromServices] RestablecerContrasenaCasoUso casoUso,
        Guid id, RestablecerContrasenaSolicitud solicitud, CancellationToken ct)
    {
        await casoUso.EjecutarAsync(new RestablecerContrasenaComando(ActorId(), id, solicitud.ContrasenaTemporal), ct);
        return NoContent();
    }

    private Guid ActorId() => Guid.Parse(User.FindFirst("sub")!.Value);   // del token JWT, no del cuerpo
}
