using Microsoft.EntityFrameworkCore;
using Npgsql;

namespace Sigh.Administracion.Infraestructura.Persistencia;

/// <summary>
/// Crea contextos con una conexión propia, fuera de la transacción de la petición. Se usa para los
/// registros que deben quedar guardados aunque la operación que los causó se revierta (research D3).
/// </summary>
public sealed class FabricaContextoAdministracion(NpgsqlDataSource fuenteDatos)
{
    public AdministracionDbContext CrearIndependiente()
    {
        var opciones = new DbContextOptionsBuilder<AdministracionDbContext>()
            .UseNpgsql(fuenteDatos, DependencyInjection.ConfigurarNpgsql)
            .Options;
        return new AdministracionDbContext(opciones);
    }
}
