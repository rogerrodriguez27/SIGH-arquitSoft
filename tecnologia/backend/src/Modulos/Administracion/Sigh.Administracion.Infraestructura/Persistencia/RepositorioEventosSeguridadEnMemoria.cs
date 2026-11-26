using System.Collections.Concurrent;
using Sigh.Administracion.Dominio.Contratos;
using Sigh.Administracion.Dominio.Seguridad;

namespace Sigh.Administracion.Infraestructura.Persistencia;

/// <summary>Implementación en memoria para pruebas (docs §2). Solo inserta.</summary>
public sealed class RepositorioEventosSeguridadEnMemoria : IRepositorioEventosSeguridad
{
    private readonly ConcurrentQueue<EventoSeguridad> _eventos = new();

    public IReadOnlyList<EventoSeguridad> Eventos => _eventos.ToList();

    public Task AgregarAsync(EventoSeguridad evento, CancellationToken ct = default)
    {
        _eventos.Enqueue(evento);
        return Task.CompletedTask;
    }
}
