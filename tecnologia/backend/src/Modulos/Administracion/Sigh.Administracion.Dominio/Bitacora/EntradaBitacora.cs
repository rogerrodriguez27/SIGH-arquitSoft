using Sigh.Administracion.Dominio.Roles;
using Sigh.Compartido.Tiempo;

namespace Sigh.Administracion.Dominio.Bitacora;

/// <summary>
/// Registro inmutable de un acceso o intento de acceso a una historia clínica (RF24, RN-06, FR-019).
/// No tiene campos clínicos (FR-023) ni métodos de modificación (FR-021).
/// </summary>
public sealed class EntradaBitacora
{
    public Guid Id { get; private set; }
    public Guid UsuarioId { get; private set; }

    /// <summary>Copia del nombre al momento del acceso, para leer la bitácora sin joins.</summary>
    public string NombreUsuario { get; private set; } = string.Empty;

    /// <summary>Rol con el que actuó. Si se denegó, el primer rol del usuario.</summary>
    public CodigoRol Rol { get; private set; }

    /// <summary>Hora del servidor; nunca la envía el cliente.</summary>
    public DateTimeOffset FechaHora { get; private set; }

    public string Accion { get; private set; } = string.Empty;
    public Guid HistoriaId { get; private set; }
    public string ModuloOrigen { get; private set; } = string.Empty;
    public ResultadoAcceso Resultado { get; private set; }

    private EntradaBitacora() { }                                    // requerido por EF Core

    public static EntradaBitacora Crear(Guid usuarioId, string nombreUsuario, CodigoRol rol, string accion,
        Guid historiaId, string moduloOrigen, ResultadoAcceso resultado, IReloj reloj)
    {
        if (!AccionBitacora.Todas.Contains(accion ?? string.Empty))
            throw new ArgumentException($"La acción '{accion}' no está en la lista cerrada de la bitácora.", nameof(accion));
        if (!Bitacora.ModuloOrigen.Todos.Contains(moduloOrigen ?? string.Empty))
            throw new ArgumentException($"El módulo de origen '{moduloOrigen}' no es válido.", nameof(moduloOrigen));
        if (!Enum.IsDefined(rol))
            throw new ArgumentException("El rol no es válido.", nameof(rol));
        ArgumentException.ThrowIfNullOrWhiteSpace(nombreUsuario);

        return new EntradaBitacora
        {
            Id = Guid.NewGuid(),
            UsuarioId = usuarioId,
            NombreUsuario = nombreUsuario,
            Rol = rol,
            FechaHora = reloj.Ahora,
            Accion = accion!,
            HistoriaId = historiaId,
            ModuloOrigen = moduloOrigen!,
            Resultado = resultado,
        };
    }
}
