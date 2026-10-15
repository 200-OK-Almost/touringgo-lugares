using Lugares.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Lugares.Infrastructure.Data.Configurations;

public class ControlCuotaGoogleConfiguration : IEntityTypeConfiguration<ControlCuotaGoogle>
{
    public void Configure(EntityTypeBuilder<ControlCuotaGoogle> builder)
    {
        builder.ToTable("control_cuota_google");

        builder.HasKey(c => c.Fecha);

        builder.Property(c => c.Fecha)
            .HasColumnName("fecha")
            .HasColumnType("date");

        builder.Property(c => c.LlamadasRealizadas)
            .HasColumnName("llamadas_realizadas")
            .HasDefaultValue(0)
            .IsRequired();

        builder.Property(c => c.LimiteDiario)
            .HasColumnName("limite_diario")
            .IsRequired();

        builder.Property(c => c.CircuitoAbierto)
            .HasColumnName("circuito_abierto")
            .HasDefaultValue(false)
            .IsRequired();

        builder.Property(c => c.FechaActualizacion)
            .HasColumnName("fecha_actualizacion")
            .HasDefaultValueSql("now()")
            .IsRequired();
    }
}
