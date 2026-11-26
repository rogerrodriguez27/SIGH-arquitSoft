using Sigh.Administracion.Dominio.Bitacora;

namespace Sigh.Administracion.Aplicacion.Bitacora.ConsultarBitacora;

/// <summary>Filtros de la consulta de la bitácora (HU16). ActorId viene del token.</summary>
public sealed record ConsultarBitacoraConsulta(
    Guid ActorId,
    DateTimeOffset Desde,
    DateTimeOffset Hasta,
    Guid? UsuarioId = null,
    Guid? HistoriaId = null,
    string? Accion = null,
    ResultadoAcceso? Resultado = null,
    int Pagina = 1,
    int Tamano = 20);
