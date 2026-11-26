using Sigh.Administracion.Dominio.Contratos;
using Sigh.Administracion.Dominio.Seguridad;
using Sigh.Compartido.Tiempo;

namespace Sigh.Administracion.Aplicacion.Usuarios.ReactivarUsuario;

/// <summary>Reactiva al usuario (FR-008) y registra USUARIO_REACTIVADO (FR-017).</summary>
public sealed class ReactivarUsuarioCasoUso(
    IRepositorioUsuarios usuarios,
    IRepositorioEventosSeguridad eventos,
    IReloj reloj)
{
    public async Task EjecutarAsync(ReactivarUsuarioComando comando, CancellationToken ct = default)
    {
        var usuario = await usuarios.ObtenerExistenteAsync(comando.UsuarioId, ct);

        usuario.Reactivar();
        await usuarios.ActualizarAsync(usuario, ct);

        await eventos.AgregarAsync(EventoSeguridad.Crear(TipoEventoSeguridad.UsuarioReactivado, reloj,
            comando.ActorId, usuario.Id, $"usuario: {usuario.NombreUsuario}"), ct);
    }
}
