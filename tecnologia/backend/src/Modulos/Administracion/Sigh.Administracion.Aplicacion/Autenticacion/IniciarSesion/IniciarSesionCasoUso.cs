using Sigh.Administracion.Dominio.Contratos;
using Sigh.Administracion.Dominio.Seguridad;
using Sigh.Administracion.Dominio.Usuarios;
using Sigh.Compartido.Errores;
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

        var resultado = await AutenticarYGuardarAsync(usuario, comando.Contrasena ?? string.Empty, ct);

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

    /// <summary>
    /// Si otro inicio de sesión simultáneo del mismo usuario (dos pestañas, doble clic) guardó antes, el
    /// intento se repite sobre el estado actual. Así no se pierde ningún intento fallido (FR-004) y el
    /// usuario no recibe un error por la concurrencia.
    /// </summary>
    private async Task<ResultadoAutenticacion> AutenticarYGuardarAsync(Usuario usuario, string contrasena,
        CancellationToken ct)
    {
        // La verificación del hash es lenta a propósito (PBKDF2). En los reintentos se reutiliza su
        // resultado, para que cada reintento solo cueste una lectura y una escritura.
        var hasherDelIntento = new HasherConMemoria(hasher);
        for (var intento = 1; ; intento++)
        {
            var resultado = usuario.IntentarAutenticar(contrasena, hasherDelIntento, reloj);
            try
            {
                await usuarios.ActualizarAsync(usuario, ct);
                return resultado;
            }
            catch (ConflictoError) when (intento < MaximoIntentosPorConcurrencia)
            {
                await usuarios.RecargarAsync(usuario, ct);
            }
        }
    }

    private const int MaximoIntentosPorConcurrencia = 10;

    /// <summary>Decorator: recuerda la verificación de un mismo par (hash, contraseña) dentro de un inicio de sesión.</summary>
    private sealed class HasherConMemoria(IHasherContrasenas interno) : IHasherContrasenas
    {
        private (string Hash, string Contrasena, ResultadoVerificacionContrasena Resultado)? _ultima;

        public string Calcular(string contrasena) => interno.Calcular(contrasena);

        public ResultadoVerificacionContrasena Verificar(string hash, string contrasena)
        {
            if (_ultima is { } u && u.Hash == hash && u.Contrasena == contrasena)
                return u.Resultado;
            var resultado = interno.Verificar(hash, contrasena);
            _ultima = (hash, contrasena, resultado);
            return resultado;
        }
    }

    private Task RegistrarAsync(TipoEventoSeguridad tipo, Guid? usuarioId, MotivoRechazo? motivo, CancellationToken ct) =>
        eventos.AgregarAsync(EventoSeguridad.Crear(tipo, reloj, usuarioId, usuarioId, $"motivo: {motivo}"), ct);

    private string HashReferencia() => _hashReferencia ??= hasher.Calcular(Guid.NewGuid().ToString("N"));
}
