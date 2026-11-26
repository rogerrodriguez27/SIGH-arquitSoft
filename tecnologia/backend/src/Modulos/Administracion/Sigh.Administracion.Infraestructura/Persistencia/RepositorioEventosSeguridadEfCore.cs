using Sigh.Administracion.Dominio.Contratos;
using Sigh.Administracion.Dominio.Seguridad;

namespace Sigh.Administracion.Infraestructura.Persistencia;

/// <summary>
/// Guarda eventos de seguridad con conexión y transacción propias, para que queden aunque la operación
/// que los causó se revierta (research D3). Solo inserta (research D5).
/// </summary>
internal sealed class RepositorioEventosSeguridadEfCore(FabricaContextoAdministracion fabrica)
    : IRepositorioEventosSeguridad
{
    public async Task AgregarAsync(EventoSeguridad evento, CancellationToken ct = default)
    {
        await using var contexto = fabrica.CrearIndependiente();
        contexto.EventosSeguridad.Add(evento);
        await contexto.SaveChangesAsync(ct);
    }
}
