using Lugares.Domain.Entities;
using Lugares.Domain.Enums;
using Lugares.Infrastructure.Data.Converters;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Lugares.Infrastructure.Data.Configurations;

public class ResenaLugarConfiguration : IEntityTypeConfiguration<ResenaLugar>
{
    public void Configure(EntityTypeBuilder<ResenaLugar> builder)
    {
        builder.ToTable("resenas_lugar");

        builder.HasKey(r => r.Id);

        builder.Property(r => r.Id)
            .HasColumnName("id")
            .HasDefaultValueSql("gen_random_uuid()");

        builder.Property(r => r.LugarId)
            .HasColumnName("lugar_id")
            .IsRequired();

        builder.Property(r => r.Origen)
            .HasColumnName("origen")
            .HasMaxLength(30)
            .HasConversion(EnumConverters.OrigenLugarConverter)
            .HasDefaultValue(OrigenLugar.CuradoLocal)
            .IsRequired();

        builder.Property(r => r.AutorNombre)
            .HasColumnName("autor_nombre")
            .HasMaxLength(100)
            .IsRequired(false);

        builder.Property(r => r.Puntuacion)
            .HasColumnName("puntuacion")
            .IsRequired();

        builder.Property(r => r.Comentario)
            .HasColumnName("comentario")
            .HasMaxLength(500)
            .IsRequired(false);

        builder.Property(r => r.FechaResena)
            .HasColumnName("fecha_resena")
            .HasColumnType("date")
            .IsRequired(false);

        builder.HasIndex(r => r.LugarId);

        builder.HasOne(r => r.Lugar)
            .WithMany(l => l.Resenas)
            .HasForeignKey(r => r.LugarId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
