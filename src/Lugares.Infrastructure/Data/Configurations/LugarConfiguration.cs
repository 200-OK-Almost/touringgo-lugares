using Lugares.Domain.Entities;
using Lugares.Domain.Enums;
using Lugares.Infrastructure.Data.Converters;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Lugares.Infrastructure.Data.Configurations;

public class LugarConfiguration : IEntityTypeConfiguration<Lugar>
{
    public void Configure(EntityTypeBuilder<Lugar> builder)
    {
        builder.ToTable("lugares");

        builder.HasKey(l => l.Id);

        builder.Property(l => l.Id)
            .HasColumnName("id")
            .HasDefaultValueSql("gen_random_uuid()");

        builder.Property(l => l.Origen)
            .HasColumnName("origen")
            .HasMaxLength(30)
            .HasConversion(EnumConverters.OrigenLugarConverter)
            .HasDefaultValue(OrigenLugar.CuradoLocal)
            .IsRequired();

        builder.Property(l => l.IdGooglePlace)
            .HasColumnName("id_google_place")
            .HasMaxLength(100)
            .IsRequired(false);

        builder.HasIndex(l => l.IdGooglePlace)
            .IsUnique()
            .HasFilter("id_google_place IS NOT NULL");

        builder.Property(l => l.FechaUltimaSincronizacion)
            .HasColumnName("fecha_ultima_sincronizacion")
            .IsRequired(false);

        builder.Property(l => l.TipoLugar)
            .HasColumnName("tipo_lugar")
            .HasMaxLength(30)
            .HasConversion(EnumConverters.TipoLugarConverter)
            .HasDefaultValue(TipoLugar.PuntoInteres)
            .IsRequired();

        builder.Property(l => l.Nombre)
            .HasColumnName("nombre")
            .HasMaxLength(150)
            .IsRequired();

        builder.Property(l => l.Barrio)
            .HasColumnName("barrio")
            .HasMaxLength(30)
            .HasConversion(EnumConverters.BarrioCabaConverter)
            .IsRequired();

        builder.Property(l => l.Subzona)
            .HasColumnName("subzona")
            .HasMaxLength(40)
            .IsRequired(false);

        builder.Property(l => l.Direccion)
            .HasColumnName("direccion")
            .HasMaxLength(200)
            .IsRequired();

        builder.Property(l => l.Latitud)
            .HasColumnName("latitud")
            .HasPrecision(10, 8)
            .IsRequired();

        builder.Property(l => l.Longitud)
            .HasColumnName("longitud")
            .HasPrecision(11, 8)
            .IsRequired();

        builder.Property(l => l.Descripcion)
            .HasColumnName("descripcion")
            .HasColumnType("text")
            .IsRequired(false);

        builder.Property(l => l.DuracionEstimadaMinutos)
            .HasColumnName("duracion_estimada_minutos")
            .HasDefaultValue((short)60)
            .IsRequired();

        builder.Property(l => l.NivelPrecio)
            .HasColumnName("nivel_precio")
            .HasDefaultValue((short)1)
            .IsRequired();

        builder.Property(l => l.Puntuacion)
            .HasColumnName("puntuacion")
            .HasPrecision(2, 1)
            .IsRequired(false);

        builder.Property(l => l.CantidadResenas)
            .HasColumnName("cantidad_resenas")
            .HasDefaultValue(0)
            .IsRequired();

        builder.Property(l => l.EsBajoTecho)
            .HasColumnName("es_bajo_techo")
            .HasDefaultValue(true)
            .IsRequired();

        builder.Property(l => l.EsAlAireLibre)
            .HasColumnName("es_al_aire_libre")
            .HasDefaultValue(false)
            .IsRequired();

        builder.Property(l => l.AptoNinos)
            .HasColumnName("apto_ninos")
            .HasDefaultValue(true)
            .IsRequired();

        builder.Property(l => l.AccesibleMovilidadReducida)
            .HasColumnName("accesible_movilidad_reducida")
            .HasDefaultValue(false)
            .IsRequired();

        builder.Property(l => l.OfreceMenuInfantil)
            .HasColumnName("ofrece_menu_infantil")
            .HasDefaultValue(false)
            .IsRequired();

        builder.Property(l => l.Activo)
            .HasColumnName("activo")
            .HasDefaultValue(true)
            .IsRequired();

        builder.Property(l => l.FechaCreacion)
            .HasColumnName("fecha_creacion")
            .HasDefaultValueSql("now()")
            .IsRequired();

        builder.Property(l => l.FechaActualizacion)
            .HasColumnName("fecha_actualizacion")
            .HasDefaultValueSql("now()")
            .IsRequired();

        // Índices del DER
        builder.HasIndex(l => new { l.Barrio, l.Latitud, l.Longitud })
            .HasDatabaseName("idx_lugares_geo");

        builder.HasIndex(l => new { l.Barrio, l.TipoLugar, l.EsBajoTecho })
            .HasDatabaseName("idx_lugares_filtro_catalogo");
    }
}
