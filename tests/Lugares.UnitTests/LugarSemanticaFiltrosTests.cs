using Lugares.Application.DTOs;
using Lugares.Domain.Entities;
using Lugares.Domain.Enums;
using Lugares.Infrastructure.Data;
using Lugares.Infrastructure.Repositories;
using Microsoft.EntityFrameworkCore;

namespace Lugares.UnitTests;

public class LugarSemanticaFiltrosTests
{
    [Fact]
    public void Aplicar_PorBarrio_DevuelveSoloLugaresDelBarrio()
    {
        using var context = CrearContexto();

        var resultado = LugarSemanticaFiltros
            .Aplicar(context.Lugares, new LugarBusquedaSemanticaDto { Barrio = BarrioCaba.Palermo })
            .ToList();

        Assert.Equal(2, resultado.Count);
        Assert.All(resultado, l => Assert.Equal(BarrioCaba.Palermo, l.Barrio));
    }

    [Fact]
    public void Aplicar_PorCategoria_DevuelveSoloLugaresDeEsaCategoria()
    {
        using var context = CrearContexto();

        var resultado = LugarSemanticaFiltros
            .Aplicar(context.Lugares, new LugarBusquedaSemanticaDto
            {
                CategoriaCodigos = new[] { "cafe_especialidad" }
            })
            .ToList();

        var lugar = Assert.Single(resultado);
        Assert.Equal("Café Palermo", lugar.Nombre);
    }

    [Fact]
    public void Aplicar_PorVariasCategorias_DevuelveLaUnion()
    {
        using var context = CrearContexto();

        var resultado = LugarSemanticaFiltros
            .Aplicar(context.Lugares, new LugarBusquedaSemanticaDto
            {
                CategoriaCodigos = new[] { "cafe_especialidad", "parques_aire_libre" }
            })
            .ToList();

        Assert.Equal(2, resultado.Count);
    }

    [Fact]
    public void Aplicar_PorNivelPrecioMaximo_ExcluyeLosMasCaros()
    {
        using var context = CrearContexto();

        var resultado = LugarSemanticaFiltros
            .Aplicar(context.Lugares, new LugarBusquedaSemanticaDto { NivelPrecioMaximo = 1 })
            .ToList();

        var lugar = Assert.Single(resultado);
        Assert.Equal("Parque Palermo", lugar.Nombre);
    }

    [Fact]
    public void Aplicar_SoloAlAireLibre_DevuelveSoloEsosLugares()
    {
        using var context = CrearContexto();

        var resultado = LugarSemanticaFiltros
            .Aplicar(context.Lugares, new LugarBusquedaSemanticaDto { SoloAlAireLibre = true })
            .ToList();

        var lugar = Assert.Single(resultado);
        Assert.True(lugar.EsAlAireLibre);
    }

    [Fact]
    public void Aplicar_SoloBajoTecho_DevuelveSoloEsosLugares()
    {
        using var context = CrearContexto();

        var resultado = LugarSemanticaFiltros
            .Aplicar(context.Lugares, new LugarBusquedaSemanticaDto { SoloBajoTecho = true })
            .ToList();

        Assert.Equal(3, resultado.Count);
        Assert.All(resultado, l => Assert.True(l.EsBajoTecho));
    }

    [Fact]
    public void Aplicar_AptoNinos_DevuelveSoloLugaresFamiliares()
    {
        using var context = CrearContexto();

        var resultado = LugarSemanticaFiltros
            .Aplicar(context.Lugares, new LugarBusquedaSemanticaDto { AptoNinos = true })
            .ToList();

        Assert.Equal(3, resultado.Count);
        Assert.All(resultado, l => Assert.True(l.AptoNinos));
    }

    [Fact]
    public void Aplicar_AccesibleMovilidadReducida_DevuelveSoloAccesibles()
    {
        using var context = CrearContexto();

        var resultado = LugarSemanticaFiltros
            .Aplicar(context.Lugares, new LugarBusquedaSemanticaDto { AccesibleMovilidadReducida = true })
            .ToList();

        Assert.Equal(2, resultado.Count);
        Assert.All(resultado, l => Assert.True(l.AccesibleMovilidadReducida));
    }

    [Fact]
    public void Aplicar_RestriccionDietaria_DevuelveSoloLugaresCompatibles()
    {
        using var context = CrearContexto();

        var resultado = LugarSemanticaFiltros
            .Aplicar(context.Lugares, new LugarBusquedaSemanticaDto
            {
                RestriccionDietaria = RestriccionDietaria.Celiaco
            })
            .ToList();

        var lugar = Assert.Single(resultado);
        Assert.Equal("Bar San Telmo", lugar.Nombre);
    }

    [Fact]
    public void Aplicar_ExcluirIds_QuitaLosLugaresIndicados()
    {
        using var context = CrearContexto();
        var cafe = context.Lugares.Single(l => l.Nombre == "Café Palermo");
        var bar = context.Lugares.Single(l => l.Nombre == "Bar San Telmo");

        var resultado = LugarSemanticaFiltros
            .Aplicar(context.Lugares, new LugarBusquedaSemanticaDto
            {
                ExcluirIds = new[] { cafe.Id, bar.Id }
            })
            .ToList();

        Assert.Equal(2, resultado.Count);
        Assert.DoesNotContain(resultado, l => l.Id == cafe.Id);
        Assert.DoesNotContain(resultado, l => l.Id == bar.Id);
    }

    private static ApplicationDbContext CrearContexto()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        var context = new InMemoryApplicationDbContext(options);

        var cafe = new CategoriaLugar { Id = 1, Codigo = "cafe_especialidad", Nombre = "Café" };
        var parque = new CategoriaLugar { Id = 2, Codigo = "parques_aire_libre", Nombre = "Parque" };
        var gastro = new CategoriaLugar { Id = 3, Codigo = "gastronomia_clasica", Nombre = "Gastronomía" };
        var museo = new CategoriaLugar { Id = 4, Codigo = "museos_galerias", Nombre = "Museo" };
        context.CategoriasLugar.AddRange(cafe, parque, gastro, museo);

        var cafePalermo = new Lugar
        {
            Id = Guid.NewGuid(),
            Nombre = "Café Palermo",
            Barrio = BarrioCaba.Palermo,
            NivelPrecio = 2,
            EsBajoTecho = true,
            EsAlAireLibre = false,
            AptoNinos = true,
            AccesibleMovilidadReducida = false,
            Activo = true
        };

        var parquePalermo = new Lugar
        {
            Id = Guid.NewGuid(),
            Nombre = "Parque Palermo",
            Barrio = BarrioCaba.Palermo,
            NivelPrecio = 1,
            EsBajoTecho = false,
            EsAlAireLibre = true,
            AptoNinos = true,
            AccesibleMovilidadReducida = true,
            Activo = true
        };

        var barSanTelmo = new Lugar
        {
            Id = Guid.NewGuid(),
            Nombre = "Bar San Telmo",
            Barrio = BarrioCaba.SanTelmo,
            NivelPrecio = 2,
            EsBajoTecho = true,
            EsAlAireLibre = false,
            AptoNinos = false,
            AccesibleMovilidadReducida = false,
            Activo = true
        };

        var museoRecoleta = new Lugar
        {
            Id = Guid.NewGuid(),
            Nombre = "Museo Recoleta",
            Barrio = BarrioCaba.Recoleta,
            NivelPrecio = 3,
            EsBajoTecho = true,
            EsAlAireLibre = false,
            AptoNinos = true,
            AccesibleMovilidadReducida = true,
            Activo = true
        };

        context.Lugares.AddRange(cafePalermo, parquePalermo, barSanTelmo, museoRecoleta);
        context.SaveChanges();

        context.LugaresCategorias.AddRange(
            new LugarCategoria { LugarId = cafePalermo.Id, CategoriaId = cafe.Id, Categoria = cafe },
            new LugarCategoria { LugarId = parquePalermo.Id, CategoriaId = parque.Id, Categoria = parque },
            new LugarCategoria { LugarId = barSanTelmo.Id, CategoriaId = gastro.Id, Categoria = gastro },
            new LugarCategoria { LugarId = museoRecoleta.Id, CategoriaId = museo.Id, Categoria = museo });

        context.LugaresOpcionesDietarias.AddRange(
            new LugarOpcionDietaria
            {
                LugarId = cafePalermo.Id,
                Restriccion = RestriccionDietaria.Vegetariano
            },
            new LugarOpcionDietaria
            {
                LugarId = barSanTelmo.Id,
                Restriccion = RestriccionDietaria.Celiaco
            });

        context.SaveChanges();
        return context;
    }
}
