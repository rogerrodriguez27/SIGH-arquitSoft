using System.Text;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using Sigh.Administracion.Dominio.Bitacora;
using Sigh.Administracion.Dominio.Seguridad;
using Sigh.Administracion.Dominio.Usuarios;
using Sigh.Administracion.Infraestructura.Persistencia.Configuraciones;
using Sigh.Compartido.Persistencia;

namespace Sigh.Administracion.Infraestructura.Persistencia;

/// <summary>
/// Contexto de EF Core del módulo Administración. Usa solo el esquema "administracion" (principio II)
/// y, cuando hay una transacción activa en la unidad de trabajo, se une a ella antes de guardar
/// (research D3, FR-022).
/// </summary>
public sealed class AdministracionDbContext : DbContext
{
    public const string Esquema = "administracion";

    private readonly ConexionOltp? _conexion;

    public AdministracionDbContext(DbContextOptions<AdministracionDbContext> opciones, ConexionOltp? conexion = null)
        : base(opciones)
    {
        _conexion = conexion;
    }

    public DbSet<EventoSeguridad> EventosSeguridad => Set<EventoSeguridad>();
    public DbSet<Usuario> Usuarios => Set<Usuario>();
    public DbSet<EntradaBitacora> EntradasBitacora => Set<EntradaBitacora>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.HasDefaultSchema(Esquema);
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(AdministracionDbContext).Assembly);
        AplicarNombresSnakeCase(modelBuilder);
    }

    public override async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        // Se une a la transacción compartida activa; si ya terminó, deja de usarla (varias transacciones por petición).
        if (_conexion is not null)
        {
            var compartida = _conexion.Transaccion;
            if (!ReferenceEquals(Database.CurrentTransaction?.GetDbTransaction(), compartida))
                await Database.UseTransactionAsync(compartida, cancellationToken);
        }

        IncrementarVersiones();
        return await base.SaveChangesAsync(cancellationToken);
    }

    /// <summary>Concurrencia optimista: cada usuario modificado sube su columna version.</summary>
    private void IncrementarVersiones()
    {
        foreach (var entrada in ChangeTracker.Entries<Usuario>().Where(e => e.State == EntityState.Modified))
        {
            var version = entrada.Property<int>(UsuarioConfiguracion.PropiedadVersion);
            version.CurrentValue = version.OriginalValue + 1;
        }
    }

    /// <summary>Convierte a snake_case los nombres de columnas que no se definieron a mano.</summary>
    private static void AplicarNombresSnakeCase(ModelBuilder modelBuilder)
    {
        foreach (var entidad in modelBuilder.Model.GetEntityTypes())
        {
            foreach (var propiedad in entidad.GetProperties())
            {
                if (propiedad.GetColumnName() == propiedad.Name)
                    propiedad.SetColumnName(ASnakeCase(propiedad.Name));
            }
        }
    }

    internal static string ASnakeCase(string nombre)
    {
        var resultado = new StringBuilder();
        for (var i = 0; i < nombre.Length; i++)
        {
            if (i > 0 && char.IsUpper(nombre[i]))
                resultado.Append('_');
            resultado.Append(char.ToLowerInvariant(nombre[i]));
        }
        return resultado.ToString();
    }
}
