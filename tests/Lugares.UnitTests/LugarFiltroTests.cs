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

        var context = new ApplicationDbContext(options);

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
}
