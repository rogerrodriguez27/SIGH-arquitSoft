using Sigh.Administracion.Dominio.Contratos;
using Sigh.Administracion.Dominio.Seguridad;
using Sigh.Administracion.Dominio.Usuarios;
using Sigh.Compartido.Errores;
using Sigh.Compartido.Tiempo;

namespace Sigh.Administracion.Aplicacion.Autenticacion.CambiarContrasena;

/// <summary>
/// Cambio de la contraseña propia (FR-005, FR-006). Entrega una sesión nueva sin el cambio pendiente y
/// registra CONTRASENA_CAMBIADA (FR-026).
/// </summary>
public sealed class CambiarContrasenaCasoUso(
    IRepositorioUsuarios usuarios,
    IRepositorioEventosSeguridad eventos,
    IHasherContrasenas hasher,
    IEmisorTokens emisor,
    IReloj reloj)
{
    public async Task<SesionResultado> EjecutarAsync(CambiarContrasenaComando comando, CancellationToken ct = default)
    {
        var usuario = await usuarios.ObtenerPorIdAsync(comando.UsuarioId, ct)
            ?? throw new RecursoNoEncontradoError("El usuario no existe.");

        var nueva = ContrasenaPropuesta.Crear(comando.ContrasenaNueva);
        usuario.CambiarContrasena(comando.ContrasenaActual, nueva, hasher);
        await usuarios.ActualizarAsync(usuario, ct);

        await eventos.AgregarAsync(
            EventoSeguridad.Crear(TipoEventoSeguridad.ContrasenaCambiada, reloj, usuario.Id, usuario.Id), ct);
        return SesionResultado.Emitir(usuario, emisor);
    }
}
