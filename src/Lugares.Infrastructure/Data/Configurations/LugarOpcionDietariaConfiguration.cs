using Lugares.Domain.Entities;
using Lugares.Infrastructure.Data.Converters;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Lugares.Infrastructure.Data.Configurations;

public class LugarOpcionDietariaConfiguration : IEntityTypeConfiguration<LugarOpcionDietaria>
{
    public void Configure(EntityTypeBuilder<LugarOpcionDietaria> builder)
    {
        builder.ToTable("lugar_opciones_dietarias");

        builder.HasKey(lod => new { lod.LugarId, lod.Restriccion });

        builder.Property(lod => lod.LugarId)
            .HasColumnName("lugar_id")
            .IsRequired();

        builder.Property(lod => lod.Restriccion)
            .HasColumnName("restriccion")
            .HasMaxLength(30)
            .HasConversion(EnumConverters.RestriccionDietariaConverter)
            .IsRequired();

        builder.HasOne(lod => lod.Lugar)
            .WithMany(l => l.OpcionesDietarias)
            .HasForeignKey(lod => lod.LugarId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
