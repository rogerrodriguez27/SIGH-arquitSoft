using Microsoft.EntityFrameworkCore;
using Npgsql;
using Sigh.Administracion.Infraestructura.Persistencia;
using Testcontainers.PostgreSql;

namespace Sigh.Administracion.Integracion.Tests.Soporte;

/// <summary>
/// PostgreSQL 17 real en un contenedor (research D10), con las migraciones aplicadas. Las credenciales
/// son del contenedor temporal de prueba, no del sistema.
/// </summary>
public sealed class PostgresFixture : IAsyncLifetime
{
    private readonly PostgreSqlContainer _contenedor = new PostgreSqlBuilder("postgres:17")
        .WithDatabase("sigh_oltp")
        .WithCommand("-c", "max_connections=300")      // la prueba de concurrencia abre muchas conexiones a la vez
        .Build();

    public NpgsqlDataSource FuenteDatos { get; private set; } = null!;

    public string CadenaConexion => _contenedor.GetConnectionString();

    public async Task InitializeAsync()
    {
        await _contenedor.StartAsync();
        FuenteDatos = NpgsqlDataSource.Create(_contenedor.GetConnectionString());
        await using var contexto = CrearContexto();
        await contexto.Database.MigrateAsync();
    }

    public async Task DisposeAsync()
    {
        await FuenteDatos.DisposeAsync();
        await _contenedor.DisposeAsync();
    }

    public AdministracionDbContext CrearContexto() => new FabricaContextoAdministracion(FuenteDatos).CrearIndependiente();
}

[CollectionDefinition(Nombre)]
public sealed class ColeccionPostgres : ICollectionFixture<PostgresFixture>
{
    public const string Nombre = "PostgreSQL";
}
