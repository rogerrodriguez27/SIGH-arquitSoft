using Sigh.Compartido.Paginacion;

namespace Sigh.Administracion.Api.Respuestas;

/// <summary>Página de resultados (contratos PaginaUsuarios y PaginaBitacora).</summary>
public sealed record PaginaRespuesta<T>(IReadOnlyList<T> Elementos, int Pagina, int Tamano, int Total)
{
    public static PaginaRespuesta<T> Desde(Pagina<T> pagina) =>
        new(pagina.Elementos, pagina.NumeroPagina, pagina.Tamano, pagina.Total);
}
