using Microsoft.EntityFrameworkCore;
using Sigh.Administracion.Dominio.Bitacora;
using Sigh.Administracion.Dominio.Contratos;
using Sigh.Compartido.Paginacion;

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

    /// <summary>Consulta con los índices de research D8; PostgreSQL solo acepta fechas en UTC.</summary>
    public async Task<Pagina<EntradaBitacora>> ConsultarAsync(FiltroBitacora filtro, int pagina, int tamano,
        CancellationToken ct = default)
    {
        var desde = filtro.Desde.ToUniversalTime();
        var hasta = filtro.Hasta.ToUniversalTime();
        var consulta = contexto.EntradasBitacora.AsNoTracking()
            .Where(e => e.FechaHora >= desde && e.FechaHora <= hasta);

        if (filtro.UsuarioId is { } usuarioId)
            consulta = consulta.Where(e => e.UsuarioId == usuarioId);
        if (filtro.HistoriaId is { } historiaId)
            consulta = consulta.Where(e => e.HistoriaId == historiaId);
        if (!string.IsNullOrEmpty(filtro.Accion))
            consulta = consulta.Where(e => e.Accion == filtro.Accion);
        if (filtro.Resultado is { } resultado)
            consulta = consulta.Where(e => e.Resultado == resultado);

        var total = await consulta.CountAsync(ct);
        var elementos = await consulta
            .OrderByDescending(e => e.FechaHora).ThenByDescending(e => e.Id)
            .Skip((pagina - 1) * tamano).Take(tamano)
            .ToListAsync(ct);

        return new Pagina<EntradaBitacora>(elementos, pagina, tamano, total);
    }
}
