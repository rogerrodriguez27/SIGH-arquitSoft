namespace Sigh.Compartido.Tiempo;

/// <summary>Puerto para obtener la hora del servidor. Permite usar una hora fija en las pruebas.</summary>
public interface IReloj
{
    DateTimeOffset Ahora { get; }
}
