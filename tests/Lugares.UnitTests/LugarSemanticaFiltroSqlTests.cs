using Lugares.Application.DTOs;
using Lugares.Infrastructure.Data;
using Lugares.Infrastructure.Repositories;
using Microsoft.EntityFrameworkCore;

namespace Lugares.UnitTests;

public class LugarSemanticaFiltroSqlTests
{
    [Fact]
    public void Aplicar_ConRadio_GeneraFiltroEspacial()
    {
        using var context = CrearContextoNpgsql();

        var query = LugarSemanticaFiltros.Aplicar(
            context.Lugares,
            new LugarBusquedaSemanticaDto
            {
                LatitudReferencia = -34.6037m,
                LongitudReferencia = -58.3816m,
                RadioMetrosMaximo = 800
            });

        var sql = query.ToQueryString().ToUpperInvariant();

        Assert.Contains("ST_DWITHIN", sql);
        Assert.Contains("UBICACION", sql);
    }

    [Fact]
    public void Aplicar_SinRadio_NoGeneraFiltroEspacial()
    {
        using var context = CrearContextoNpgsql();

        var query = LugarSemanticaFiltros.Aplicar(
            context.Lugares,
            new LugarBusquedaSemanticaDto
            {
                LatitudReferencia = -34.6037m,
                LongitudReferencia = -58.3816m
            });

        var sql = query.ToQueryString().ToUpperInvariant();

        Assert.DoesNotContain("ST_DWITHIN", sql);
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
