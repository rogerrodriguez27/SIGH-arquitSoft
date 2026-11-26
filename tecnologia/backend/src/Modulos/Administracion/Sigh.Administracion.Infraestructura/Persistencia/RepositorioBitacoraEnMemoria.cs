using System.Collections.Concurrent;
using Sigh.Administracion.Dominio.Bitacora;
using Sigh.Administracion.Dominio.Contratos;
using Sigh.Compartido.Paginacion;

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

    public Task<Pagina<EntradaBitacora>> ConsultarAsync(FiltroBitacora filtro, int pagina, int tamano,
        CancellationToken ct = default)
    {
        var lista = _entradas
            .Where(e => e.FechaHora >= filtro.Desde && e.FechaHora <= filtro.Hasta)
            .Where(e => filtro.UsuarioId is null || e.UsuarioId == filtro.UsuarioId)
            .Where(e => filtro.HistoriaId is null || e.HistoriaId == filtro.HistoriaId)
            .Where(e => string.IsNullOrEmpty(filtro.Accion) || e.Accion == filtro.Accion)
            .Where(e => filtro.Resultado is null || e.Resultado == filtro.Resultado)
            .OrderByDescending(e => e.FechaHora).ThenByDescending(e => e.Id)
            .ToList();

        var elementos = lista.Skip((pagina - 1) * tamano).Take(tamano).ToList();
        return Task.FromResult(new Pagina<EntradaBitacora>(elementos, pagina, tamano, lista.Count));
    }
}
