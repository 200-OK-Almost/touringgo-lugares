using Lugares.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Lugares.Infrastructure.Data.Configurations;

public class LugarCategoriaConfiguration : IEntityTypeConfiguration<LugarCategoria>
{
    public void Configure(EntityTypeBuilder<LugarCategoria> builder)
    {
        builder.ToTable("lugar_categorias");

        builder.HasKey(lc => new { lc.LugarId, lc.CategoriaId });

        builder.Property(lc => lc.LugarId)
            .HasColumnName("lugar_id")
            .IsRequired();

        builder.Property(lc => lc.CategoriaId)
            .HasColumnName("categoria_id")
            .IsRequired();

        builder.Property(lc => lc.EsPrincipal)
            .HasColumnName("es_principal")
            .HasDefaultValue(false)
            .IsRequired();

        builder.HasOne(lc => lc.Lugar)
            .WithMany(l => l.Categorias)
            .HasForeignKey(lc => lc.LugarId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(lc => lc.Categoria)
            .WithMany(c => c.LugaresCategorias)
            .HasForeignKey(lc => lc.CategoriaId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(lc => lc.CategoriaId);
    }
}
