using Sigh.Compartido.Tiempo;

namespace Sigh.Administracion.Dominio.Seguridad;

/// <summary>
/// Registro inmutable de un evento de seguridad (FR-017, FR-025, FR-026). No tiene métodos de
/// modificación (FR-021). El detalle nunca lleva contraseñas, tokens ni datos clínicos.
/// </summary>
public sealed class EventoSeguridad
{
    public const int LongitudMaximaDetalle = 500;

    public Guid Id { get; private set; }
    public DateTimeOffset FechaHora { get; private set; }
    public TipoEventoSeguridad Tipo { get; private set; }
    public Guid? ActorId { get; private set; }
    public Guid? ObjetivoId { get; private set; }
    public string Detalle { get; private set; } = string.Empty;

    private EventoSeguridad() { }                                    // requerido por EF Core

    public static EventoSeguridad Crear(TipoEventoSeguridad tipo, IReloj reloj,
        Guid? actorId = null, Guid? objetivoId = null, string detalle = "")
    {
        ArgumentNullException.ThrowIfNull(detalle);
        if (detalle.Length > LongitudMaximaDetalle)
            throw new ArgumentException(
                $"El detalle no puede superar {LongitudMaximaDetalle} caracteres.", nameof(detalle));

        return new EventoSeguridad
        {
            Id = Guid.NewGuid(),
            FechaHora = reloj.Ahora,
            Tipo = tipo,
            ActorId = actorId,
            ObjetivoId = objetivoId,
            Detalle = detalle,
        };
    }
}
