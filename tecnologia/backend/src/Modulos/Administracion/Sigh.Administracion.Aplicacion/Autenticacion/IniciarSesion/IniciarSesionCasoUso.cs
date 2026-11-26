using Sigh.Administracion.Dominio.Contratos;
using Sigh.Administracion.Dominio.Seguridad;
using Sigh.Administracion.Dominio.Usuarios;
using Sigh.Compartido.Tiempo;

namespace Sigh.Administracion.Aplicacion.Autenticacion.IniciarSesion;

/// <summary>
/// Inicio de sesión (RF21, FR-001 a FR-004, FR-026). Devuelve null ante cualquier rechazo, para que la
/// API responda siempre el mismo mensaje genérico; el evento de seguridad sí guarda el motivo.
/// </summary>
public sealed class IniciarSesionCasoUso(
    IRepositorioUsuarios usuarios,
    IRepositorioEventosSeguridad eventos,
    IHasherContrasenas hasher,
    IEmisorTokens emisor,
    IReloj reloj)
{
    private static string? _hashReferencia;

    public async Task<SesionResultado?> EjecutarAsync(IniciarSesionComando comando, CancellationToken ct = default)
    {
        var nombre = Usuario.NormalizarNombreUsuario(comando.NombreUsuario);
        var usuario = await usuarios.ObtenerPorNombreUsuarioAsync(nombre, ct);

        if (usuario is null)
        {
            // Se calcula igual un hash para que el tiempo de respuesta no revele si el usuario existe (research D6).
            hasher.Verificar(HashReferencia(), comando.Contrasena ?? string.Empty);
            await RegistrarAsync(TipoEventoSeguridad.InicioSesionFallido, null, MotivoRechazo.UsuarioInexistente, ct);
            return null;
        }

        var resultado = usuario.IntentarAutenticar(comando.Contrasena ?? string.Empty, hasher, reloj);
        await usuarios.ActualizarAsync(usuario, ct);

        if (!resultado.Exitoso)
        {
            await RegistrarAsync(TipoEventoSeguridad.InicioSesionFallido, usuario.Id, resultado.Motivo, ct);
            if (resultado.CuentaBloqueadaAhora)
            {
                await eventos.AgregarAsync(EventoSeguridad.Crear(TipoEventoSeguridad.CuentaBloqueada, reloj,
                    usuario.Id, usuario.Id, $"bloqueada hasta {usuario.BloqueadoHasta:O}"), ct);
            }
            return null;
        }

        await eventos.AgregarAsync(
            EventoSeguridad.Crear(TipoEventoSeguridad.InicioSesionExitoso, reloj, usuario.Id, usuario.Id), ct);
        return SesionResultado.Emitir(usuario, emisor);
    }

    private Task RegistrarAsync(TipoEventoSeguridad tipo, Guid? usuarioId, MotivoRechazo? motivo, CancellationToken ct) =>
        eventos.AgregarAsync(EventoSeguridad.Crear(tipo, reloj, usuarioId, usuarioId, $"motivo: {motivo}"), ct);

    private string HashReferencia() => _hashReferencia ??= hasher.Calcular(Guid.NewGuid().ToString("N"));
}
