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
