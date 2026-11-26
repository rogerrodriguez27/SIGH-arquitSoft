using Sigh.Compartido.Tiempo;

namespace Sigh.Administracion.Aplicacion.Tests.Dobles;

/// <summary>Reloj con hora fija que se puede adelantar.</summary>
public sealed class RelojFijo(DateTimeOffset ahora) : IReloj
{
    public DateTimeOffset Ahora { get; private set; } = ahora;

    public static RelojFijo Nuevo() => new(new DateTimeOffset(2026, 11, 26, 8, 0, 0, TimeSpan.Zero));

    public void Avanzar(TimeSpan tiempo) => Ahora += tiempo;
}
