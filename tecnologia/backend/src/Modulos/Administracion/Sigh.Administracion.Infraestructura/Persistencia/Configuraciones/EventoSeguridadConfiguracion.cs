using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Sigh.Administracion.Dominio.Seguridad;

namespace Sigh.Administracion.Infraestructura.Persistencia.Configuraciones;

/// <summary>Tabla eventos_seguridad (data-model.md §2).</summary>
internal sealed class EventoSeguridadConfiguracion : IEntityTypeConfiguration<EventoSeguridad>
{
    public void Configure(EntityTypeBuilder<EventoSeguridad> builder)
    {
        builder.ToTable("eventos_seguridad");
        builder.HasKey(e => e.Id);
        builder.Property(e => e.Id).ValueGeneratedNever();
        builder.Property(e => e.FechaHora).IsRequired();
        builder.Property(e => e.Tipo)
            .HasConversion(t => t.ACodigo(), codigo => TipoEventoSeguridadExtensiones.DesdeCodigo(codigo))
            .HasMaxLength(40)
            .IsRequired();
        builder.Property(e => e.Detalle).HasMaxLength(EventoSeguridad.LongitudMaximaDetalle).IsRequired();
        builder.HasIndex(e => e.FechaHora).IsDescending().HasDatabaseName("ix_eventos_seguridad_fecha_hora");
    }
}
