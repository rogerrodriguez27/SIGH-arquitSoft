using Sigh.Administracion.Dominio.Contratos;
using Sigh.Administracion.Dominio.Seguridad;
using Sigh.Administracion.Dominio.Usuarios;
using Sigh.Compartido.Tiempo;

namespace Sigh.Administracion.Aplicacion.Usuarios.CambiarRoles;

/// <summary>
/// Reemplaza los roles del usuario (FR-010, FR-014). Los nuevos roles aplican en su siguiente sesión.
/// Registra ROLES_CAMBIADOS con el antes y el después (FR-017).
/// </summary>
public sealed class CambiarRolesCasoUso(
    IRepositorioUsuarios usuarios,
    IRepositorioEventosSeguridad eventos,
    IReloj reloj)
{
    public async Task<UsuarioDto> EjecutarAsync(CambiarRolesComando comando, CancellationToken ct = default)
    {
        var usuario = await usuarios.ObtenerExistenteAsync(comando.UsuarioId, ct);
        var anteriores = string.Join(", ", usuario.Roles.Order());

        PoliticaAdministradores.ValidarCambioRoles(comando.ActorId, usuario, comando.Roles,
            await usuarios.ContarAdministradoresActivosAsync(ct));
        usuario.AsignarRoles(comando.Roles);
        await usuarios.ActualizarAsync(usuario, ct);

        await eventos.AgregarAsync(EventoSeguridad.Crear(TipoEventoSeguridad.RolesCambiados, reloj,
            comando.ActorId, usuario.Id, $"roles: {anteriores} → {string.Join(", ", usuario.Roles.Order())}"), ct);
        return UsuarioDto.Desde(usuario, reloj.Ahora);
    }
}
