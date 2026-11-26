namespace Sigh.Compartido.Tiempo;

/// <summary>Hora real del servidor, en UTC.</summary>
public sealed class RelojSistema : IReloj
{
    public DateTimeOffset Ahora => DateTimeOffset.UtcNow;
}
