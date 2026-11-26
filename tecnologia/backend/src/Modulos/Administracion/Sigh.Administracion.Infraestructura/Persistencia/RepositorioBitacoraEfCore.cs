using Sigh.Administracion.Dominio.Bitacora;
using Sigh.Administracion.Dominio.Contratos;

namespace Sigh.Administracion.Infraestructura.Persistencia;

/// <summary>Bitácora en PostgreSQL. Solo inserta (research D5).</summary>
internal sealed class RepositorioBitacoraEfCore(AdministracionDbContext contexto, FabricaContextoAdministracion fabrica)
    : IRepositorioBitacora
{
    /// <summary>Usa el contexto de la petición, que se une a la transacción compartida (research D3).</summary>
    public async Task AgregarAsync(EntradaBitacora entrada, CancellationToken ct = default)
    {
        contexto.EntradasBitacora.Add(entrada);
        await contexto.SaveChangesAsync(ct);
    }

    /// <summary>Conexión y transacción propias: queda guardado aunque la operación se revierta (FR-020).</summary>
    public async Task AgregarEnTransaccionPropiaAsync(EntradaBitacora entrada, CancellationToken ct = default)
    {
        await using var independiente = fabrica.CrearIndependiente();
        independiente.EntradasBitacora.Add(entrada);
        await independiente.SaveChangesAsync(ct);
    }
}
