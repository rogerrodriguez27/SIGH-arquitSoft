using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Sigh.Administracion.Dominio.Bitacora;

namespace Sigh.Administracion.Infraestructura.Persistencia.Configuraciones;

/// <summary>
/// Tabla bitacora_accesos_hce (data-model.md §2). historia_id no tiene FK hacia el esquema hce: las
/// referencias entre módulos son solo identificadores (principio II).
/// </summary>
internal sealed class EntradaBitacoraConfiguracion : IEntityTypeConfiguration<EntradaBitacora>
{
    public void Configure(EntityTypeBuilder<EntradaBitacora> builder)
    {
        builder.ToTable("bitacora_accesos_hce");
        builder.HasKey(e => e.Id);
        builder.Property(e => e.Id).ValueGeneratedNever();
        builder.Property(e => e.NombreUsuario).HasMaxLength(50).IsRequired();
        builder.Property(e => e.Rol).HasConversion<string>().HasMaxLength(30).IsRequired();
        builder.Property(e => e.FechaHora).IsRequired();
        builder.Property(e => e.Accion).HasMaxLength(40).IsRequired();
        builder.Property(e => e.ModuloOrigen).HasMaxLength(20).IsRequired();
        builder.Property(e => e.Resultado).HasConversion<string>().HasMaxLength(10).IsRequired();

        // Índices para la consulta de la HU16 (research D8).
        builder.HasIndex(e => e.FechaHora).IsDescending()
            .HasDatabaseName("ix_bitacora_accesos_hce_fecha_hora");
        builder.HasIndex(e => new { e.UsuarioId, e.FechaHora }).IsDescending(false, true)
            .HasDatabaseName("ix_bitacora_accesos_hce_usuario_fecha_hora");
        builder.HasIndex(e => new { e.HistoriaId, e.FechaHora }).IsDescending(false, true)
            .HasDatabaseName("ix_bitacora_accesos_hce_historia_fecha_hora");
    }
}
