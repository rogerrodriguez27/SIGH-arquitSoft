using Sigh.Administracion.Dominio.Bitacora;
using Sigh.Compartido.Paginacion;

namespace Sigh.Administracion.Dominio.Contratos;

/// <summary>Puerto de la bitácora de accesos a la HCE: inserción y consulta, nunca modificación (FR-021, research D5).</summary>
public interface IRepositorioBitacora
{
    /// <summary>Inserta dentro de la transacción compartida activa (FR-022, research D3).</summary>
    Task AgregarAsync(EntradaBitacora entrada, CancellationToken ct = default);

    /// <summary>Inserta con conexión y transacción propias, para que quede aunque la operación se revierta (FR-020).</summary>
    Task AgregarEnTransaccionPropiaAsync(EntradaBitacora entrada, CancellationToken ct = default);

    /// <summary>Entradas que cumplen todos los filtros, de la más reciente a la más antigua (FR-024).</summary>
    Task<Pagina<EntradaBitacora>> ConsultarAsync(FiltroBitacora filtro, int pagina, int tamano, CancellationToken ct = default);
}

/// <summary>Filtros de la consulta de la bitácora (HU16). El rango Desde–Hasta es inclusivo.</summary>
public sealed record FiltroBitacora(
    DateTimeOffset Desde,
    DateTimeOffset Hasta,
    Guid? UsuarioId = null,
    Guid? HistoriaId = null,
    string? Accion = null,
    ResultadoAcceso? Resultado = null);
