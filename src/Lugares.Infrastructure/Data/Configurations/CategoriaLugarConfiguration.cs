using Lugares.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Lugares.Infrastructure.Data.Configurations;

public class CategoriaLugarConfiguration : IEntityTypeConfiguration<CategoriaLugar>
{
    public void Configure(EntityTypeBuilder<CategoriaLugar> builder)
    {
        builder.ToTable("categorias_lugar");

        builder.HasKey(c => c.Id);

        builder.Property(c => c.Id)
            .HasColumnName("id")
            .ValueGeneratedOnAdd();

        builder.Property(c => c.Codigo)
            .HasColumnName("codigo")
            .HasMaxLength(40)
            .IsRequired();

        builder.HasIndex(c => c.Codigo)
            .IsUnique();

        builder.Property(c => c.Nombre)
            .HasColumnName("nombre")
            .HasMaxLength(80)
            .IsRequired();
    }
}
