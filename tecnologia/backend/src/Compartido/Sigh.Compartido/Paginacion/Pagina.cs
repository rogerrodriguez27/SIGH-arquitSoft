namespace Sigh.Compartido.Paginacion;

/// <summary>Una página de resultados.</summary>
public sealed record Pagina<T>(IReadOnlyList<T> Elementos, int NumeroPagina, int Tamano, int Total);
