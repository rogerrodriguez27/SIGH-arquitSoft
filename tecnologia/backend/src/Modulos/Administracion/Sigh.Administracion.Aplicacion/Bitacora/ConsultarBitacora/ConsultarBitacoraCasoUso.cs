using Sigh.Administracion.Dominio.Bitacora;
using Sigh.Administracion.Dominio.Contratos;
using Sigh.Administracion.Dominio.Seguridad;
using Sigh.Compartido.Errores;
using Sigh.Compartido.Paginacion;
using Sigh.Compartido.Tiempo;

namespace Sigh.Administracion.Aplicacion.Bitacora.ConsultarBitacora;

/// <summary>
/// Consulta de la bitácora por el administrador (HU16, FR-024): rango obligatorio de un año o menos,
/// filtros combinables, paginación y orden de la más reciente a la más antigua. Cada consulta queda
/// registrada como BITACORA_CONSULTADA (FR-025).
/// </summary>
public sealed class ConsultarBitacoraCasoUso(
    IRepositorioBitacora bitacora,
    IRepositorioEventosSeguridad eventos,
    IReloj reloj)
{
    public const int TamanoMaximo = 100;

    public async Task<Pagina<EntradaBitacoraDto>> EjecutarAsync(ConsultarBitacoraConsulta consulta,
        CancellationToken ct = default)
    {
        Validar(consulta);

        var pagina = await bitacora.ConsultarAsync(new FiltroBitacora(consulta.Desde, consulta.Hasta,
            consulta.UsuarioId, consulta.HistoriaId, consulta.Accion, consulta.Resultado), consulta.Pagina, consulta.Tamano, ct);

        await eventos.AgregarAsync(EventoSeguridad.Crear(TipoEventoSeguridad.BitacoraConsultada, reloj,
            consulta.ActorId, detalle: DescribirFiltros(consulta)), ct);

        return new Pagina<EntradaBitacoraDto>(pagina.Elementos.Select(EntradaBitacoraDto.Desde).ToList(),
            pagina.NumeroPagina, pagina.Tamano, pagina.Total);
    }

    private static void Validar(ConsultarBitacoraConsulta consulta)
    {
        if (consulta.Desde > consulta.Hasta)
            throw new ReglaNegocioError("La fecha inicial no puede ser posterior a la fecha final.");
        if (consulta.Desde.AddYears(1) < consulta.Hasta)
            throw new ReglaNegocioError("El rango de fechas no puede ser mayor a un año.");
        if (consulta.Pagina < 1 || consulta.Tamano < 1 || consulta.Tamano > TamanoMaximo)
            throw new ReglaNegocioError($"La página debe ser 1 o mayor y el tamaño entre 1 y {TamanoMaximo}.");
        if (!string.IsNullOrEmpty(consulta.Accion) && !AccionBitacora.Todas.Contains(consulta.Accion))
            throw new ReglaNegocioError($"La acción '{consulta.Accion}' no existe.");
    }

    /// <summary>Detalle del evento: los filtros usados, sin datos clínicos (FR-025).</summary>
    private static string DescribirFiltros(ConsultarBitacoraConsulta c)
    {
        var partes = new List<string> { $"desde={c.Desde:O}", $"hasta={c.Hasta:O}" };
        if (c.UsuarioId is { } usuario) partes.Add($"usuario={usuario}");
        if (c.HistoriaId is { } historia) partes.Add($"historia={historia}");
        if (!string.IsNullOrEmpty(c.Accion)) partes.Add($"accion={c.Accion}");
        if (c.Resultado is { } resultado) partes.Add($"resultado={resultado}");
        partes.Add($"pagina={c.Pagina}");
        return "filtros: " + string.Join(", ", partes);
    }
}
