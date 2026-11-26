using Sigh.Administracion.Dominio.Contratos;
using Sigh.Administracion.Dominio.Seguridad;
using Sigh.Administracion.Dominio.Usuarios;
using Sigh.Compartido.Tiempo;

namespace Sigh.Administracion.Aplicacion.Usuarios.DesactivarUsuario;

/// <summary>
/// Desactiva al usuario, sin eliminarlo (FR-008, FR-009, FR-014). Registra USUARIO_DESACTIVADO (FR-017).
/// </summary>
public sealed class DesactivarUsuarioCasoUso(
    IRepositorioUsuarios usuarios,
    IRepositorioEventosSeguridad eventos,
    IReloj reloj)
{
    public async Task EjecutarAsync(DesactivarUsuarioComando comando, CancellationToken ct = default)
    {
        var usuario = await usuarios.ObtenerExistenteAsync(comando.UsuarioId, ct);

        PoliticaAdministradores.ValidarDesactivacion(comando.ActorId, usuario,
            await usuarios.ContarAdministradoresActivosAsync(ct));
        usuario.Desactivar();
        await usuarios.ActualizarAsync(usuario, ct);

        await eventos.AgregarAsync(EventoSeguridad.Crear(TipoEventoSeguridad.UsuarioDesactivado, reloj,
            comando.ActorId, usuario.Id, $"usuario: {usuario.NombreUsuario}"), ct);
    }
}
