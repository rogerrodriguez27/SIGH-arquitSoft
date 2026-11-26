using Sigh.Administracion.Dominio.Contratos;
using Sigh.Administracion.Dominio.Seguridad;
using Sigh.Administracion.Dominio.Usuarios;
using Sigh.Compartido.Tiempo;

namespace Sigh.Administracion.Aplicacion.Usuarios.RestablecerContrasena;

/// <summary>
/// Asigna una contraseña temporal y exige cambiarla en el siguiente inicio de sesión (FR-015, FR-006).
/// Registra CONTRASENA_RESTABLECIDA sin la contraseña (FR-026).
/// </summary>
public sealed class RestablecerContrasenaCasoUso(
    IRepositorioUsuarios usuarios,
    IRepositorioEventosSeguridad eventos,
    IHasherContrasenas hasher,
    IReloj reloj)
{
    public async Task EjecutarAsync(RestablecerContrasenaComando comando, CancellationToken ct = default)
    {
        var usuario = await usuarios.ObtenerExistenteAsync(comando.UsuarioId, ct);

        usuario.RestablecerContrasena(ContrasenaPropuesta.Crear(comando.ContrasenaTemporal), hasher);
        await usuarios.ActualizarAsync(usuario, ct);

        await eventos.AgregarAsync(EventoSeguridad.Crear(TipoEventoSeguridad.ContrasenaRestablecida, reloj,
            comando.ActorId, usuario.Id, $"usuario: {usuario.NombreUsuario}"), ct);
    }
}
