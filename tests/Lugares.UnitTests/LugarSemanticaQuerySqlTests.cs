using Lugares.Application.DTOs;
using Lugares.Infrastructure.Data;
using Lugares.Infrastructure.Repositories;
using Microsoft.EntityFrameworkCore;
using Pgvector;

namespace Lugares.UnitTests;

public class LugarSemanticaQuerySqlTests
{
    [Fact]
    public void Construir_ConUmbral_AgregaCondicionDeSimilitud()
    {
        using var context = CrearContextoNpgsql();
        var queryVector = new Vector(new float[1024]);

        var sinUmbral = LugarSemanticaQuery
            .Construir(context.Lugares, queryVector, new LugarBusquedaSemanticaDto { Cantidad = 5 })
            .ToQueryString();

        var conUmbral = LugarSemanticaQuery
            .Construir(context.Lugares, queryVector, new LugarBusquedaSemanticaDto
            {
                Cantidad = 5,
                SimilitudMinima = 0.6
            })
            .ToQueryString();

        Assert.Contains("<=>", sinUmbral);
        Assert.Contains("<=>", conUmbral);
        Assert.NotEqual(sinUmbral, conUmbral);
        Assert.Contains("WHERE 1.0 -", conUmbral);
        Assert.Contains("@__minima", conUmbral);
    }

    [Fact]
    public void Construir_SinUmbral_NoAgregaCondicionDeSimilitud()
    {
        using var context = CrearContextoNpgsql();
        var queryVector = new Vector(new float[1024]);

        var sql = LugarSemanticaQuery
            .Construir(context.Lugares, queryVector, new LugarBusquedaSemanticaDto { Cantidad = 5 })
            .ToQueryString();

        Assert.Contains("<=>", sql);
        Assert.DoesNotContain("WHERE 1.0 -", sql);
    }

    [Fact]
    public void Construir_ConTodosLosFiltros_TraduceSinErrores()
    {
        using var context = CrearContextoNpgsql();
        var queryVector = new Vector(new float[1024]);

        var sql = LugarSemanticaQuery
            .Construir(context.Lugares, queryVector, new LugarBusquedaSemanticaDto
            {
                Query = "lugar al aire libre",
                Cantidad = 5,
                Barrio = Lugares.Domain.Enums.BarrioCaba.Palermo,
                CategoriaCodigos = new[] { "cafe_especialidad" },
                NivelPrecioMaximo = 2,
                SoloAlAireLibre = true,
                SoloBajoTecho = true,
                AptoNinos = true,
                AccesibleMovilidadReducida = true,
                RestriccionDietaria = Lugares.Domain.Enums.RestriccionDietaria.Celiaco,
                ExcluirIds = new[] { Guid.NewGuid() },
                LatitudReferencia = -34.6037m,
                LongitudReferencia = -58.3816m,
                RadioMetrosMaximo = 800,
                SimilitudMinima = 0.5
            })
            .ToQueryString()
            .ToUpperInvariant();

        Assert.Contains("ST_DWITHIN", sql);
        Assert.Contains("NIVEL_PRECIO", sql);
        Assert.Contains("ES_AL_AIRE_LIBRE", sql);
        Assert.Contains("ES_BAJO_TECHO", sql);
        Assert.Contains("APTO_NINOS", sql);
        Assert.Contains("ACCESIBLE_MOVILIDAD_REDUCIDA", sql);
        Assert.Contains("RESTRICCION", sql);
        Assert.Contains("WHERE 1.0 -", sql);
    }

    private static ApplicationDbContext CrearContextoNpgsql()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseNpgsql(
                "Host=localhost;Port=5432;Database=lugares_test;Username=postgres;Password=postgres",
                npgsqlOptions =>
                {
                    npgsqlOptions.UseNetTopologySuite();
                    npgsqlOptions.UseVector();
                })
            .Options;

        return new ApplicationDbContext(options);
    }
}
