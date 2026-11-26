using Microsoft.EntityFrameworkCore;
using Npgsql.EntityFrameworkCore.PostgreSQL.Infrastructure;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Npgsql;
using Sigh.Administracion.Aplicacion.Bitacora;
using Sigh.Administracion.Aplicacion.Publico;
using Sigh.Administracion.Dominio.Contratos;
using Sigh.Administracion.Infraestructura.Persistencia;
using Sigh.Administracion.Infraestructura.Seguridad;
using Sigh.Compartido.Persistencia;
using Sigh.Compartido.Tiempo;

namespace Sigh.Administracion.Infraestructura;

/// <summary>Registro de los adaptadores del módulo Administración (docs §6.5, principio I).</summary>
public static class DependencyInjection
{
    public const string NombreCadenaConexion = "SighOltp";

    public static IServiceCollection AgregarModuloAdministracion(this IServiceCollection services,
        IConfiguration configuracion)
    {
        // Piezas compartidas por todos los módulos (research D3). TryAdd: otro módulo puede registrarlas antes.
        services.TryAddSingleton(_ => NpgsqlDataSource.Create(
            configuracion.GetConnectionString(NombreCadenaConexion)
            ?? throw new InvalidOperationException(
                "Falta la variable de entorno ConnectionStrings__SighOltp.")));
        services.TryAddScoped<ConexionOltp>();
        services.TryAddScoped<IUnidadDeTrabajo, UnidadDeTrabajoNpgsql>();
        services.TryAddSingleton<IReloj, RelojSistema>();

        services.AddDbContext<AdministracionDbContext>((sp, opciones) =>
            opciones.UseNpgsql(sp.GetRequiredService<ConexionOltp>().Conexion, ConfigurarNpgsql));
        services.AddSingleton<FabricaContextoAdministracion>();

        services.AddScoped<IRepositorioEventosSeguridad, RepositorioEventosSeguridadEfCore>();
        services.AddScoped<IRepositorioUsuarios, RepositorioUsuariosEfCore>();
        services.AddScoped<IRepositorioBitacora, RepositorioBitacoraEfCore>();

        // Contrato público para los demás módulos (FR-018, contracts/servicio-bitacora.md)
        services.AddScoped<IServicioBitacora, ServicioBitacora>();

        // Seguridad: hash de contraseñas y emisión de JWT (research D2, D4)
        services.AddSingleton<IHasherContrasenas, HasherContrasenasIdentity>();
        services.AddSingleton<IEmisorTokens, EmisorTokensJwt>();

        return services;
    }

    /// <summary>La tabla de historial de migraciones también vive en el esquema del módulo.</summary>
    internal static void ConfigurarNpgsql(NpgsqlDbContextOptionsBuilder npgsql) =>
        npgsql.MigrationsHistoryTable("historial_migraciones", AdministracionDbContext.Esquema);
}
