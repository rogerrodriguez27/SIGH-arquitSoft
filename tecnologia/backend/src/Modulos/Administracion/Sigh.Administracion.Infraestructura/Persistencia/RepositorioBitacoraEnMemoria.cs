using System.Collections.Concurrent;
using Sigh.Administracion.Dominio.Bitacora;
using Sigh.Administracion.Dominio.Contratos;

namespace Sigh.Administracion.Infraestructura.Persistencia;

/// <summary>Implementación en memoria para pruebas (docs §2). Solo inserta.</summary>
public sealed class RepositorioBitacoraEnMemoria : IRepositorioBitacora
{
    private readonly ConcurrentQueue<EntradaBitacora> _entradas = new();
    private readonly ConcurrentDictionary<Guid, bool> _enTransaccionPropia = new();

    public IReadOnlyList<EntradaBitacora> Entradas => _entradas.ToList();

    /// <summary>Indica si la entrada se guardó con <see cref="AgregarEnTransaccionPropiaAsync"/>.</summary>
    public bool EnTransaccionPropia(Guid id) => _enTransaccionPropia.GetValueOrDefault(id);

    public Task AgregarAsync(EntradaBitacora entrada, CancellationToken ct = default)
    {
        _entradas.Enqueue(entrada);
        return Task.CompletedTask;
    }

    public Task AgregarEnTransaccionPropiaAsync(EntradaBitacora entrada, CancellationToken ct = default)
    {
        _entradas.Enqueue(entrada);
        _enTransaccionPropia[entrada.Id] = true;
        return Task.CompletedTask;
    }
}
