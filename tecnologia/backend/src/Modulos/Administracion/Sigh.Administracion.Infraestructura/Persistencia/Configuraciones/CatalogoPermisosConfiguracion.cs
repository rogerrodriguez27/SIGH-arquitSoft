using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Sigh.Administracion.Dominio.Permisos;
using Sigh.Administracion.Dominio.Roles;

namespace Sigh.Administracion.Infraestructura.Persistencia.Configuraciones;

/// <summary>
/// Tablas de catálogo roles, permisos y rol_permisos (data-model.md §2). Son una copia de solo lectura de
/// la matriz fija para los informes SQL; la aplicación decide los permisos con MatrizPermisos.
/// </summary>
internal sealed class FilaRol
{
    public string Codigo { get; set; } = string.Empty;
    public string Nombre { get; set; } = string.Empty;
    public string Descripcion { get; set; } = string.Empty;
}

internal sealed class FilaPermiso
{
    public string Codigo { get; set; } = string.Empty;
    public string Descripcion { get; set; } = string.Empty;
}

internal sealed class FilaRolPermiso
{
    public string Rol { get; set; } = string.Empty;
    public string Permiso { get; set; } = string.Empty;
}

internal sealed class FilaRolConfiguracion : IEntityTypeConfiguration<FilaRol>
{
    public void Configure(EntityTypeBuilder<FilaRol> builder)
    {
        builder.ToTable("roles");
        builder.HasKey(r => r.Codigo);
        builder.Property(r => r.Codigo).HasMaxLength(30);
        builder.Property(r => r.Nombre).HasMaxLength(60).IsRequired();
        builder.Property(r => r.Descripcion).HasMaxLength(200).IsRequired();
        builder.HasData(CatalogoRoles.Todos.Select(r => new FilaRol
        {
            Codigo = r.Codigo.ToString(),
            Nombre = r.Nombre,
            Descripcion = r.Descripcion,
        }));
    }
}

internal sealed class FilaPermisoConfiguracion : IEntityTypeConfiguration<FilaPermiso>
{
    public void Configure(EntityTypeBuilder<FilaPermiso> builder)
    {
        builder.ToTable("permisos");
        builder.HasKey(p => p.Codigo);
        builder.Property(p => p.Codigo).HasMaxLength(40);
        builder.Property(p => p.Descripcion).HasMaxLength(200).IsRequired();
        builder.HasData(CodigoPermiso.Todos.Select(p => new FilaPermiso { Codigo = p.Codigo, Descripcion = p.Descripcion }));
    }
}

internal sealed class FilaRolPermisoConfiguracion : IEntityTypeConfiguration<FilaRolPermiso>
{
    public void Configure(EntityTypeBuilder<FilaRolPermiso> builder)
    {
        builder.ToTable("rol_permisos");
        builder.HasKey(rp => new { rp.Rol, rp.Permiso });
        builder.HasOne<FilaRol>().WithMany().HasForeignKey(rp => rp.Rol).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<FilaPermiso>().WithMany().HasForeignKey(rp => rp.Permiso).OnDelete(DeleteBehavior.Restrict);
        builder.HasData(MatrizPermisos.Filas().SelectMany(f => f.Roles.Select(r => new FilaRolPermiso
        {
            Rol = r.ToString(),
            Permiso = f.Codigo,
        })));
    }
}
