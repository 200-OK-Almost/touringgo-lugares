using Lugares.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Lugares.Infrastructure.Data.Configurations;

public class FotoLugarConfiguration : IEntityTypeConfiguration<FotoLugar>
{
    public void Configure(EntityTypeBuilder<FotoLugar> builder)
    {
        builder.ToTable("fotos_lugar");

        builder.HasKey(f => f.Id);

        builder.Property(f => f.Id)
            .HasColumnName("id")
            .HasDefaultValueSql("gen_random_uuid()");

        builder.Property(f => f.LugarId)
            .HasColumnName("lugar_id")
            .IsRequired();

        builder.Property(f => f.Url)
            .HasColumnName("url")
            .HasMaxLength(255)
            .IsRequired();

        builder.Property(f => f.Orden)
            .HasColumnName("orden")
            .HasDefaultValue((short)1)
            .IsRequired();

        builder.Property(f => f.EsPortada)
            .HasColumnName("es_portada")
            .HasDefaultValue(false)
            .IsRequired();

        builder.HasIndex(f => new { f.LugarId, f.Orden })
            .IsUnique();

        builder.HasOne(f => f.Lugar)
            .WithMany(l => l.Fotos)
            .HasForeignKey(f => f.LugarId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
