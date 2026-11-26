using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Authorization;
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
[Authorize(Policy = CodigoPermiso.GestionarUsuarios)]
public sealed class UsuariosController : ControllerBase
{
    [HttpGet]
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
    public async Task<ActionResult<UsuarioDto>> Crear([FromServices] CrearUsuarioCasoUso casoUso,
        CrearUsuarioSolicitud solicitud, CancellationToken ct)
    {
        var usuario = await casoUso.EjecutarAsync(new CrearUsuarioComando(ActorId(), solicitud.NombreUsuario,
            solicitud.NombreCompleto, solicitud.ContrasenaInicial, solicitud.Roles), ct);
        return Created($"/api/v1/usuarios/{usuario.Id}", usuario);
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<UsuarioDto>> Consultar([FromServices] ConsultarUsuarioCasoUso casoUso,
        Guid id, CancellationToken ct) =>
        Ok(await casoUso.EjecutarAsync(id, ct));

    [HttpPut("{id:guid}")]
    public async Task<ActionResult<UsuarioDto>> Editar([FromServices] EditarUsuarioCasoUso casoUso,
        Guid id, EditarUsuarioSolicitud solicitud, CancellationToken ct) =>
        Ok(await casoUso.EjecutarAsync(new EditarUsuarioComando(ActorId(), id, solicitud.NombreCompleto), ct));

    [HttpPut("{id:guid}/roles")]
    public async Task<ActionResult<UsuarioDto>> CambiarRoles([FromServices] CambiarRolesCasoUso casoUso,
        Guid id, CambiarRolesSolicitud solicitud, CancellationToken ct) =>
        Ok(await casoUso.EjecutarAsync(new CambiarRolesComando(ActorId(), id, solicitud.Roles), ct));

    [HttpPost("{id:guid}/desactivar")]
    public async Task<IActionResult> Desactivar([FromServices] DesactivarUsuarioCasoUso casoUso,
        Guid id, CancellationToken ct)
    {
        await casoUso.EjecutarAsync(new DesactivarUsuarioComando(ActorId(), id), ct);
        return NoContent();
    }

    [HttpPost("{id:guid}/reactivar")]
    public async Task<IActionResult> Reactivar([FromServices] ReactivarUsuarioCasoUso casoUso,
        Guid id, CancellationToken ct)
    {
        await casoUso.EjecutarAsync(new ReactivarUsuarioComando(ActorId(), id), ct);
        return NoContent();
    }

    [HttpPost("{id:guid}/restablecer-contrasena")]
    public async Task<IActionResult> RestablecerContrasena([FromServices] RestablecerContrasenaCasoUso casoUso,
        Guid id, RestablecerContrasenaSolicitud solicitud, CancellationToken ct)
    {
        await casoUso.EjecutarAsync(new RestablecerContrasenaComando(ActorId(), id, solicitud.ContrasenaTemporal), ct);
        return NoContent();
    }

    private Guid ActorId() => Guid.Parse(User.FindFirst("sub")!.Value);   // del token JWT, no del cuerpo
}
