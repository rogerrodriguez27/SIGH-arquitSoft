using Sigh.Administracion.Dominio.Contratos;
using Sigh.Administracion.Dominio.Seguridad;
using Sigh.Compartido.Tiempo;

namespace Sigh.Administracion.Aplicacion.Usuarios.EditarUsuario;

/// <summary>Edita los datos básicos del usuario (FR-008) y registra USUARIO_EDITADO (FR-017).</summary>
public sealed class EditarUsuarioCasoUso(
    IRepositorioUsuarios usuarios,
    IRepositorioEventosSeguridad eventos,
    IReloj reloj)
{
    public async Task<UsuarioDto> EjecutarAsync(EditarUsuarioComando comando, CancellationToken ct = default)
    {
        var usuario = await usuarios.ObtenerExistenteAsync(comando.UsuarioId, ct);
        var anterior = usuario.NombreCompleto;

        usuario.EditarDatos(comando.NombreCompleto);
        await usuarios.ActualizarAsync(usuario, ct);

        await eventos.AgregarAsync(EventoSeguridad.Crear(TipoEventoSeguridad.UsuarioEditado, reloj,
            comando.ActorId, usuario.Id, $"nombre completo: {anterior} → {usuario.NombreCompleto}"), ct);
        return UsuarioDto.Desde(usuario, reloj.Ahora);
    }
}
