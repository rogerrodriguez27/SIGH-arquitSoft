using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Sigh.Administracion.Dominio.Roles;
using Sigh.Administracion.Dominio.Usuarios;

namespace Sigh.Administracion.Infraestructura.Persistencia.Configuraciones;

/// <summary>Tablas usuarios y usuario_roles (data-model.md §2).</summary>
internal sealed class UsuarioConfiguracion : IEntityTypeConfiguration<Usuario>
{
    public const string PropiedadVersion = "Version";

    public void Configure(EntityTypeBuilder<Usuario> builder)
    {
        builder.ToTable("usuarios", t => t.HasCheckConstraint(
            "ck_usuarios_estado", "estado IN ('Activo','Desactivado')"));

        builder.HasKey(u => u.Id);
        builder.Property(u => u.Id).ValueGeneratedNever();
        builder.Property(u => u.NombreUsuario).HasMaxLength(Usuario.LongitudMaximaNombreUsuario).IsRequired();
        builder.HasIndex(u => u.NombreUsuario).IsUnique().HasDatabaseName("ux_usuarios_nombre_usuario");
        builder.Property(u => u.NombreCompleto).HasMaxLength(Usuario.LongitudMaximaNombreCompleto).IsRequired();
        builder.Property(u => u.HashContrasena).IsRequired();
        builder.Property(u => u.Estado).HasConversion<string>().HasMaxLength(20).IsRequired();
        builder.Property(u => u.CreadoEn).IsRequired();

        // Token de concurrencia optimista: lo incrementa AdministracionDbContext en cada actualización.
        builder.Property<int>(PropiedadVersion).IsConcurrencyToken().HasDefaultValue(0);

        builder.Ignore(u => u.Roles);
        builder.HasMany(u => u.RolesAsignados)
            .WithOne()
            .HasForeignKey(r => r.UsuarioId)
            .OnDelete(DeleteBehavior.Restrict);
        builder.Navigation(u => u.RolesAsignados).UsePropertyAccessMode(PropertyAccessMode.Field);
    }
}

internal sealed class RolAsignadoConfiguracion : IEntityTypeConfiguration<RolAsignado>
{
    public void Configure(EntityTypeBuilder<RolAsignado> builder)
    {
        var roles = string.Join(",", Enum.GetNames<CodigoRol>().Select(r => $"'{r}'"));
        builder.ToTable("usuario_roles", t => t.HasCheckConstraint("ck_usuario_roles_rol", $"rol IN ({roles})"));
        builder.HasKey(r => new { r.UsuarioId, r.Rol });
        builder.Property(r => r.Rol).HasConversion<string>().HasMaxLength(30);
    }
}
