namespace Sigh.Compartido.Errores;

/// <summary>Se viola una regla de negocio. La API la responde con 422.</summary>
public class ReglaNegocioError(string mensaje) : Exception(mensaje);
