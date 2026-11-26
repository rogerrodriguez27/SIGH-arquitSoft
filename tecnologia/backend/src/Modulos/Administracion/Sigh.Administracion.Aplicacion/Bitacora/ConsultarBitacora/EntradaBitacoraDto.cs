using Sigh.Administracion.Dominio.Bitacora;
using Sigh.Administracion.Dominio.Roles;

namespace Sigh.Administracion.Aplicacion.Bitacora.ConsultarBitacora;

/// <summary>
/// Entrada de la bitácora para la API (contrato EntradaBitacora). Solo quién, cuándo, qué acción, sobre
/// qué historia y con qué resultado: nunca contenido clínico (FR-023).
/// </summary>
public sealed record EntradaBitacoraDto(
    Guid Id,
    Guid UsuarioId,
    string NombreUsuario,
    CodigoRol Rol,
    DateTimeOffset FechaHora,
    string Accion,
    Guid HistoriaId,
    string ModuloOrigen,
    ResultadoAcceso Resultado)
{
    internal static EntradaBitacoraDto Desde(EntradaBitacora e) =>
        new(e.Id, e.UsuarioId, e.NombreUsuario, e.Rol, e.FechaHora, e.Accion, e.HistoriaId, e.ModuloOrigen, e.Resultado);
}
