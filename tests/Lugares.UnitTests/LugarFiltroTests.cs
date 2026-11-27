using Lugares.Application.DTOs;
using Lugares.Domain.Entities;
using Lugares.Domain.Enums;
using Lugares.Infrastructure.Data;
using Lugares.Infrastructure.Repositories;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace Lugares.UnitTests;

public class LugarFiltroTests
{
    private ApplicationDbContext CreateInMemoryDbContext()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        var context = new InMemoryApplicationDbContext(options);

        context.Lugares.AddRange(
            new Lugar
            {
                Id = Guid.NewGuid(),
                Nombre = "Café Interior",
                Barrio = BarrioCaba.Palermo,
                EsBajoTecho = true,
                EsAlAireLibre = false,
                AptoNinos = true,
                NivelPrecio = 1,
                Activo = true
            },
            new Lugar
            {
                Id = Guid.NewGuid(),
                Nombre = "Parque al Aire Libre",
                Barrio = BarrioCaba.Palermo,
                EsBajoTecho = false,
                EsAlAireLibre = true,
                AptoNinos = true,
                NivelPrecio = 1,
                Activo = true
            },
            new Lugar
            {
                Id = Guid.NewGuid(),
                Nombre = "Bar Nocturno San Telmo",
                Barrio = BarrioCaba.SanTelmo,
                EsBajoTecho = true,
                EsAlAireLibre = true,
                AptoNinos = false,
                NivelPrecio = 2,
                Activo = true
            }
        );

        context.SaveChanges();
        return context;
    }

    private ApplicationDbContext CreateRankingDbContext()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        var context = new InMemoryApplicationDbContext(options);

        context.Lugares.AddRange(
            new Lugar
            {
                Id = Guid.NewGuid(),
                Nombre = "Alta Puntuacion Pocas Resenas",
                Barrio = BarrioCaba.Palermo,
                Subzona = "Palermo Soho",
                Puntuacion = 5.0m,
                CantidadResenas = 10,
                NivelPrecio = 1,
                Activo = true
            },
            new Lugar
            {
                Id = Guid.NewGuid(),
                Nombre = "Alta Puntuacion Muchas Resenas",
                Barrio = BarrioCaba.Palermo,
                Subzona = "Palermo Soho",
                Puntuacion = 5.0m,
                CantidadResenas = 1000,
                NivelPrecio = 1,
                Activo = true
            },
            new Lugar
            {
                Id = Guid.NewGuid(),
                Nombre = "Media Puntuacion Muchas Resenas",
                Barrio = BarrioCaba.Palermo,
                Subzona = "Palermo Hollywood",
                Puntuacion = 4.5m,
                CantidadResenas = 5000,
                NivelPrecio = 2,
                Activo = true
            },
            new Lugar
            {
                Id = Guid.NewGuid(),
                Nombre = "Baja Puntuacion",
                Barrio = BarrioCaba.SanTelmo,
                Subzona = "San Telmo",
                Puntuacion = 4.0m,
                CantidadResenas = 200,
                NivelPrecio = 2,
                Activo = true
            },
            new Lugar
            {
                Id = Guid.NewGuid(),
                Nombre = "Sin Puntuacion",
                Barrio = BarrioCaba.SanTelmo,
                Subzona = "San Telmo",
                Puntuacion = null,
                CantidadResenas = 0,
                NivelPrecio = 1,
                Activo = true
            }
        );

        context.SaveChanges();
        return context;
    }

    [Fact]
    public async Task ObtenerFiltradosAsync_ModoLluvia_RetornaSoloBajoTecho()
    {
        using var context = CreateInMemoryDbContext();
        var repo = new LugarRepository(context);

        var filtro = new LugarFiltroDto { SoloBajoTecho = true };
        var (items, total) = await repo.ObtenerFiltradosAsync(filtro);

        Assert.Equal(2, total);
        Assert.Equal(2, items.Count);
        Assert.All(items, l => Assert.True(l.EsBajoTecho));
    }

    [Fact]
    public async Task ObtenerFiltradosAsync_PorBarrio_RetornaSoloLugaresDelBarrio()
    {
        using var context = CreateInMemoryDbContext();
        var repo = new LugarRepository(context);

        var filtro = new LugarFiltroDto { Barrio = BarrioCaba.SanTelmo };
        var (items, total) = await repo.ObtenerFiltradosAsync(filtro);

        Assert.Equal(1, total);
        Assert.Single(items);
        Assert.Equal("Bar Nocturno San Telmo", items[0].Nombre);
    }

    [Fact]
    public async Task ObtenerFiltradosAsync_AptoNinos_RetornaSoloLugaresFamiliares()
    {
        using var context = CreateInMemoryDbContext();
        var repo = new LugarRepository(context);

        var filtro = new LugarFiltroDto { AptoNinos = true };
        var (items, total) = await repo.ObtenerFiltradosAsync(filtro);

        Assert.Equal(2, total);
        Assert.Equal(2, items.Count);
        Assert.All(items, l => Assert.True(l.AptoNinos));
    }

    [Fact]
    public async Task ObtenerFiltradosAsync_ConPaginacion_RetornaPaginaYTotalCorrectos()
    {
        using var context = CreateInMemoryDbContext();
        var repo = new LugarRepository(context);

        var filtro = new LugarFiltroDto { Pagina = 1, Tamano = 2 };
        var (items, total) = await repo.ObtenerFiltradosAsync(filtro);

        Assert.Equal(3, total);
        Assert.Equal(2, items.Count);

        var filtroPagina2 = new LugarFiltroDto { Pagina = 2, Tamano = 2 };
        var (items2, total2) = await repo.ObtenerFiltradosAsync(filtroPagina2);

        Assert.Equal(3, total2);
        Assert.Single(items2);

        var filtroPaginaFuera = new LugarFiltroDto { Pagina = 3, Tamano = 2 };
        var (items3, total3) = await repo.ObtenerFiltradosAsync(filtroPaginaFuera);

        Assert.Equal(3, total3);
        Assert.Empty(items3);
    }

    [Fact]
    public async Task ObtenerFiltradosAsync_SinOrden_OrdenaPorNombre()
    {
        using var context = CreateRankingDbContext();
        var repo = new LugarRepository(context);

        var filtro = new LugarFiltroDto { Tamano = 10 };
        var (items, total) = await repo.ObtenerFiltradosAsync(filtro);

        Assert.Equal(5, total);
        Assert.Equal(
            new[]
            {
                "Alta Puntuacion Muchas Resenas",
                "Alta Puntuacion Pocas Resenas",
                "Baja Puntuacion",
                "Media Puntuacion Muchas Resenas",
                "Sin Puntuacion"
            },
            items.Select(l => l.Nombre));
    }

    [Fact]
    public async Task ObtenerFiltradosAsync_OrdenPuntuacion_OrdenaDescendenteYExcluyeSinRating()
    {
        using var context = CreateRankingDbContext();
        var repo = new LugarRepository(context);

        var filtro = new LugarFiltroDto { Orden = "puntuacion", Tamano = 10 };
        var (items, total) = await repo.ObtenerFiltradosAsync(filtro);

        Assert.Equal(4, total);
        Assert.Equal(
            new[]
            {
                "Alta Puntuacion Muchas Resenas",
                "Alta Puntuacion Pocas Resenas",
                "Media Puntuacion Muchas Resenas",
                "Baja Puntuacion"
            },
            items.Select(l => l.Nombre));
    }

    [Fact]
    public async Task ObtenerFiltradosAsync_OrdenResenas_OrdenaPorCantidadDeResenas()
    {
        using var context = CreateRankingDbContext();
        var repo = new LugarRepository(context);

        var filtro = new LugarFiltroDto { Orden = "resenas", Tamano = 10 };
        var (items, total) = await repo.ObtenerFiltradosAsync(filtro);

        Assert.Equal(5, total);
        Assert.Equal(
            new[]
            {
                "Media Puntuacion Muchas Resenas",
                "Alta Puntuacion Muchas Resenas",
                "Baja Puntuacion",
                "Alta Puntuacion Pocas Resenas",
                "Sin Puntuacion"
            },
            items.Select(l => l.Nombre));
    }

    [Fact]
    public async Task ObtenerFiltradosAsync_ResenasMinimas_FiltraPorCantidadDeResenas()
    {
        using var context = CreateRankingDbContext();
        var repo = new LugarRepository(context);

        var filtro = new LugarFiltroDto { ResenasMinimas = 500, Tamano = 10 };
        var (items, total) = await repo.ObtenerFiltradosAsync(filtro);

        Assert.Equal(2, total);
        Assert.All(items, l => Assert.True(l.CantidadResenas >= 500));
    }

    [Fact]
    public async Task ObtenerFiltradosAsync_PuntuacionMinima_FiltraPorEstrellas()
    {
        using var context = CreateRankingDbContext();
        var repo = new LugarRepository(context);

        var filtro = new LugarFiltroDto { PuntuacionMinima = 4.5m, Tamano = 10 };
        var (items, total) = await repo.ObtenerFiltradosAsync(filtro);

        Assert.Equal(3, total);
        Assert.All(items, l => Assert.True(l.Puntuacion >= 4.5m));
    }

    [Fact]
    public async Task ObtenerFiltradosAsync_PorSubzona_IgnoraMayusculasYCoincideParcial()
    {
        using var context = CreateRankingDbContext();
        var repo = new LugarRepository(context);

        var filtro = new LugarFiltroDto { Subzona = "SOHO", Tamano = 10 };
        var (items, total) = await repo.ObtenerFiltradosAsync(filtro);

        Assert.Equal(2, total);
        Assert.All(items, l => Assert.Contains("soho", l.Subzona!.ToLower()));
    }

    [Fact]
    public async Task ObtenerFiltradosAsync_CombinandoFiltrosDeRanking_RetornaSoloCoincidencias()
    {
        using var context = CreateRankingDbContext();
        var repo = new LugarRepository(context);

        var filtro = new LugarFiltroDto
        {
            Subzona = "soho",
            Orden = "resenas",
            ResenasMinimas = 500,
            Tamano = 10
        };

        var (items, total) = await repo.ObtenerFiltradosAsync(filtro);

        Assert.Equal(1, total);
        var item = Assert.Single(items);
        Assert.Equal("Alta Puntuacion Muchas Resenas", item.Nombre);
    }
}
