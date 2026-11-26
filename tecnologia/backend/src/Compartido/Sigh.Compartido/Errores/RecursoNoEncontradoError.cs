namespace Sigh.Compartido.Errores;

/// <summary>El recurso pedido no existe. La API lo responde con 404.</summary>
public class RecursoNoEncontradoError(string mensaje) : Exception(mensaje);
