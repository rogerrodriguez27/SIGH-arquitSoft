using Sigh.Administracion.Aplicacion.Publico;
using Sigh.Administracion.Dominio.Bitacora;
using Sigh.Administracion.Dominio.Contratos;
using Sigh.Administracion.Dominio.Roles;
using Sigh.Compartido.Persistencia;
using Sigh.Compartido.Tiempo;

namespace Sigh.Administracion.Aplicacion.Bitacora;

/// <summary>
/// Implementación de <see cref="IServicioBitacora"/>, interna al módulo (FR-018 a FR-023, research D3).
/// </summary>
internal sealed class ServicioBitacora(
    IRepositorioBitacora bitacora,
    IRepositorioUsuarios usuarios,
    IUnidadDeTrabajo unidadDeTrabajo,
    IReloj reloj) : IServicioBitacora
{
    public async Task RegistrarAccesoAsync(RegistroAcceso registro, CancellationToken ct = default)
    {
        if (!unidadDeTrabajo.TransaccionActiva)
        {
            throw new InvalidOperationException(
                "El acceso a la HCE debe registrarse dentro de la transacción de la operación (FR-022): use IUnidadDeTrabajo.");
        }

        var entrada = await CrearEntradaAsync(registro, ResultadoAcceso.Permitido, ct);
        await bitacora.AgregarAsync(entrada, ct);
    }

    public async Task RegistrarIntentoDenegadoAsync(RegistroAcceso registro, CancellationToken ct = default)
    {
        var entrada = await CrearEntradaAsync(registro, ResultadoAcceso.Denegado, ct);
        await bitacora.AgregarEnTransaccionPropiaAsync(entrada, ct);
    }

    private async Task<EntradaBitacora> CrearEntradaAsync(RegistroAcceso registro, ResultadoAcceso resultado,
        CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(registro);
        if (!Enum.GetNames<CodigoRol>().Contains(registro.Rol))                // solo nombres, nunca números
            throw new ArgumentException($"El rol '{registro.Rol}' no existe.", nameof(registro));
        var rol = Enum.Parse<CodigoRol>(registro.Rol);

        var usuario = await usuarios.ObtenerPorIdAsync(registro.UsuarioId, ct)
            ?? throw new ArgumentException("El usuario que accede no existe.", nameof(registro));

        return EntradaBitacora.Crear(registro.UsuarioId, usuario.NombreUsuario, rol, registro.Accion,
            registro.HistoriaId, registro.ModuloOrigen, resultado, reloj);
    }
}
