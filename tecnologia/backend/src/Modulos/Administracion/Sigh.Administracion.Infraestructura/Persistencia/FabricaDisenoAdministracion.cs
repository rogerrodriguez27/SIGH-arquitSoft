using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace Sigh.Administracion.Infraestructura.Persistencia;

/// <summary>
/// Solo para generar migraciones con "dotnet ef". La cadena no tiene credenciales y no se usa para
/// conectarse: la cadena real llega por ConnectionStrings__SighOltp (principio VI).
/// </summary>
internal sealed class FabricaDisenoAdministracion : IDesignTimeDbContextFactory<AdministracionDbContext>
{
    public AdministracionDbContext CreateDbContext(string[] args)
    {
        var opciones = new DbContextOptionsBuilder<AdministracionDbContext>()
            .UseNpgsql("Host=localhost;Database=sigh_oltp", DependencyInjection.ConfigurarNpgsql)
            .Options;
        return new AdministracionDbContext(opciones);
    }
}
