using Sigh.Administracion.Dominio.Contratos;
using Sigh.Administracion.Dominio.Seguridad;
using Sigh.Administracion.Dominio.Usuarios;
using Sigh.Compartido.Errores;
using Sigh.Compartido.Tiempo;

namespace Sigh.Administracion.Aplicacion.Usuarios.CrearUsuario;

/// <summary>
/// Registra un usuario activo con al menos un rol y el cambio de contraseña pendiente (FR-008, FR-010).
/// Registra USUARIO_CREADO (FR-017).
/// </summary>
public sealed class CrearUsuarioCasoUso(
    IRepositorioUsuarios usuarios,
    IRepositorioEventosSeguridad eventos,
    IHasherContrasenas hasher,
    IReloj reloj)
{
    public async Task<UsuarioDto> EjecutarAsync(CrearUsuarioComando comando, CancellationToken ct = default)
    {
        var usuario = Usuario.Crear(comando.NombreUsuario, comando.NombreCompleto,
            ContrasenaPropuesta.Crear(comando.ContrasenaInicial), comando.Roles, hasher, reloj);

        if (await usuarios.ObtenerPorNombreUsuarioAsync(usuario.NombreUsuario, ct) is not null)
            throw new ConflictoError("El nombre de usuario ya existe.");

        await usuarios.AgregarAsync(usuario, ct);
        await eventos.AgregarAsync(EventoSeguridad.Crear(TipoEventoSeguridad.UsuarioCreado, reloj,
            comando.ActorId, usuario.Id,
            $"usuario: {usuario.NombreUsuario}; roles: {string.Join(", ", usuario.Roles.Order())}"), ct);

        return UsuarioDto.Desde(usuario, reloj.Ahora);
    }
}
