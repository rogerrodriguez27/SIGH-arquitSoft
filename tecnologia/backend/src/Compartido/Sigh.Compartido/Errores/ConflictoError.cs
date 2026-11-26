namespace Sigh.Compartido.Errores;

/// <summary>El recurso ya existe (por ejemplo, un nombre de usuario duplicado). La API lo responde con 409.</summary>
public class ConflictoError(string mensaje) : Exception(mensaje);
