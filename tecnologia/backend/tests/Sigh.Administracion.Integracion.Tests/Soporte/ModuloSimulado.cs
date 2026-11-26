using Microsoft.EntityFrameworkCore;
using Sigh.Administracion.Aplicacion.Bitacora;
using Sigh.Administracion.Aplicacion.Publico;
using Sigh.Administracion.Infraestructura.Persistencia;
using Sigh.Compartido.Persistencia;
using Sigh.Compartido.Tiempo;

namespace Sigh.Administracion.Integracion.Tests.Soporte;

/// <summary>
/// "Módulo simulado" (contracts/servicio-bitacora.md): tiene su propio DbContext y su propia tabla en el
/// esquema modulo_simulado, como lo tendrá la HCE. Solo conoce el contrato IServicioBitacora.
/// </summary>
public sealed class ModuloSimuladoDbContext(DbContextOptions<ModuloSimuladoDbContext> opciones, ConexionOltp conexion)
    : DbContext(opciones)
{
    public DbSet<RegistroSimulado> Registros => Set<RegistroSimulado>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.HasDefaultSchema("modulo_simulado");
        modelBuilder.Entity<RegistroSimulado>(e =>
        {
            e.ToTable("registros");
            e.HasKey(r => r.Id);
            e.Property(r => r.Id).HasColumnName("id");
            e.Property(r => r.Texto).HasColumnName("texto");
        });
    }

    /// <summary>Como cada DbContext de cada módulo: se une a la transacción compartida (research D3).</summary>
    public override async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        if (conexion.Transaccion is { } transaccion && Database.CurrentTransaction is null)
            await Database.UseTransactionAsync(transaccion, cancellationToken);
        return await base.SaveChangesAsync(cancellationToken);
    }
}

public sealed class RegistroSimulado
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string Texto { get; set; } = string.Empty;
}

/// <summary>Una "petición": una conexión compartida, la unidad de trabajo, el módulo y la bitácora.</summary>
public sealed class PeticionSimulada : IAsyncDisposable
{
    private readonly ConexionOltp _conexion;

    public IUnidadDeTrabajo UnidadDeTrabajo { get; }
    public ModuloSimuladoDbContext Modulo { get; }
    public IServicioBitacora Bitacora { get; }

    public PeticionSimulada(PostgresFixture bd)
    {
        _conexion = new ConexionOltp(bd.FuenteDatos);
        UnidadDeTrabajo = new UnidadDeTrabajoNpgsql(_conexion);
        Modulo = new ModuloSimuladoDbContext(
            new DbContextOptionsBuilder<ModuloSimuladoDbContext>().UseNpgsql(_conexion.Conexion).Options, _conexion);

        var administracion = new AdministracionDbContext(
            new DbContextOptionsBuilder<AdministracionDbContext>()
                .UseNpgsql(_conexion.Conexion, Infraestructura.DependencyInjection.ConfigurarNpgsql).Options,
            _conexion);
        var fabrica = new FabricaContextoAdministracion(bd.FuenteDatos);
        Bitacora = new ServicioBitacora(new RepositorioBitacoraEfCore(administracion, fabrica),
            new RepositorioUsuariosEfCore(administracion), UnidadDeTrabajo, new RelojSistema());
    }

    public async ValueTask DisposeAsync()
    {
        await Modulo.DisposeAsync();
        await _conexion.DisposeAsync();
    }
}
