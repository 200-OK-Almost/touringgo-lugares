using Lugares.Application.DTOs;
using Lugares.Application.Interfaces;
using Lugares.Application.Services;
using Lugares.Domain.Entities;
using Lugares.Domain.Enums;
using Moq;
using Xunit;

namespace Lugares.UnitTests;

public class HaversineDistanceTests
{
    private readonly Mock<ILugarRepository> _repoMock;
    private readonly Mock<IEmbeddingClient> _embeddingClientMock;
    private readonly LugarService _service;

    public HaversineDistanceTests()
    {
        _repoMock = new Mock<ILugarRepository>();
        _embeddingClientMock = new Mock<IEmbeddingClient>();

        _service = new LugarService(_repoMock.Object, _embeddingClientMock.Object);
    }

    [Fact]
    public async Task BuscarLugaresAsync_ConCoordenadasReferencia_CalculaDistanciaCorrectamente()
    {
        // Obelisco: -34.6037, -58.3816
        // Teatro Colón: -34.6011, -58.3831 (~320m)
        var colon = new Lugar
        {
            Id = Guid.NewGuid(),
            Nombre = "Teatro Colón",
            Barrio = BarrioCaba.SanNicolas,
            Latitud = -34.6011m,
            Longitud = -58.3831m,
            Activo = true
        };

        _repoMock.Setup(r => r.ObtenerFiltradosAsync(It.IsAny<LugarFiltroDto>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((new List<Lugar> { colon }, 1));

        var filtro = new LugarFiltroDto
        {
            LatitudReferencia = -34.6037m,
            LongitudReferencia = -58.3816m
        };

        var resultado = await _service.BuscarLugaresAsync(filtro);
        var items = resultado.Items.ToList();

        Assert.Single(items);
        Assert.NotNull(items[0].DistanciaMetros);
        // Debe estar en un rango razonable de 250 a 400 metros
        Assert.InRange(items[0].DistanciaMetros!.Value, 250, 400);
    }

    [Fact]
    public async Task BuscarLugaresAsync_ConCoordenadasReferencia_OrdenaDeMasCercanoAMasLejano()
    {
        // Punto de referencia: Obelisco (-34.6037, -58.3816)
        // 1. Cercano: Teatro Colón (-34.6011, -58.3831) ~320m
        // 2. Medio: Plaza Francia (-34.5855, -58.3912) ~2.2km
        // 3. Lejano: Rosedal de Palermo (-34.5714, -58.4172) ~4.8km
        var rosedal = new Lugar { Id = Guid.NewGuid(), Nombre = "Rosedal", Latitud = -34.5714m, Longitud = -58.4172m, Activo = true };
        var colon = new Lugar { Id = Guid.NewGuid(), Nombre = "Teatro Colón", Latitud = -34.6011m, Longitud = -58.3831m, Activo = true };
        var plazaFrancia = new Lugar { Id = Guid.NewGuid(), Nombre = "Plaza Francia", Latitud = -34.5855m, Longitud = -58.3912m, Activo = true };

        _repoMock.Setup(r => r.ObtenerFiltradosAsync(It.IsAny<LugarFiltroDto>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((new List<Lugar> { rosedal, colon, plazaFrancia }, 3));

        var filtro = new LugarFiltroDto
        {
            LatitudReferencia = -34.6037m,
            LongitudReferencia = -58.3816m
        };

        var resultado = await _service.BuscarLugaresAsync(filtro);
        var items = resultado.Items.ToList();

        Assert.Equal(3, items.Count);
        Assert.Equal("Teatro Colón", items[0].Nombre);
        Assert.Equal("Plaza Francia", items[1].Nombre);
        Assert.Equal("Rosedal", items[2].Nombre);
        Assert.True(items[0].DistanciaMetros < items[1].DistanciaMetros);
        Assert.True(items[1].DistanciaMetros < items[2].DistanciaMetros);
    }

    [Fact]
    public async Task BuscarLugaresAsync_ConRadioMaximo_ExcluyeLugaresFueraDelRadio()
    {
        var colon = new Lugar { Id = Guid.NewGuid(), Nombre = "Teatro Colón", Latitud = -34.6011m, Longitud = -58.3831m, Activo = true }; // ~320m
        var rosedal = new Lugar { Id = Guid.NewGuid(), Nombre = "Rosedal", Latitud = -34.5714m, Longitud = -58.4172m, Activo = true }; // ~4.8km

        _repoMock.Setup(r => r.ObtenerFiltradosAsync(It.IsAny<LugarFiltroDto>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((new List<Lugar> { colon, rosedal }, 2));

        var filtro = new LugarFiltroDto
        {
            LatitudReferencia = -34.6037m,
            LongitudReferencia = -58.3816m,
            RadioMetrosMaximo = 1000 // Máximo 1 km
        };

        var resultado = await _service.BuscarLugaresAsync(filtro);
        var items = resultado.Items.ToList();

        Assert.Single(items);
        Assert.Equal("Teatro Colón", items[0].Nombre);
    }

    [Fact]
    public async Task BuscarLugaresAsync_PropagaMetadatosDePaginacion()
    {
        var colon = new Lugar { Id = Guid.NewGuid(), Nombre = "Teatro Colón", Latitud = -34.6011m, Longitud = -58.3831m, Activo = true };

        _repoMock.Setup(r => r.ObtenerFiltradosAsync(It.IsAny<LugarFiltroDto>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((new List<Lugar> { colon }, 35));

        var filtro = new LugarFiltroDto { Pagina = 2, Tamano = 20 };

        var resultado = await _service.BuscarLugaresAsync(filtro);

        Assert.Equal(35, resultado.Total);
        Assert.Equal(2, resultado.Pagina);
        Assert.Equal(20, resultado.Tamano);
        Assert.Single(resultado.Items);
    }

    [Fact]
    public async Task BuscarLugaresAsync_ConCoordenadasYRadio_PaginaSobreResultadoFiltradoYOrdenado()
    {
        // Referencia: Obelisco. Distancias aproximadas: Colón ~320m, Plaza Francia ~2.2km, Rosedal ~4.8km.
        var colon = new Lugar { Id = Guid.NewGuid(), Nombre = "Teatro Colón", Latitud = -34.6011m, Longitud = -58.3831m, Activo = true };
        var plazaFrancia = new Lugar { Id = Guid.NewGuid(), Nombre = "Plaza Francia", Latitud = -34.5855m, Longitud = -58.3912m, Activo = true };
        var rosedal = new Lugar { Id = Guid.NewGuid(), Nombre = "Rosedal", Latitud = -34.5714m, Longitud = -58.4172m, Activo = true };

        // Con cercanía, el repositorio entrega el conjunto completo (sin paginar).
        _repoMock.Setup(r => r.ObtenerFiltradosAsync(It.IsAny<LugarFiltroDto>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((new List<Lugar> { rosedal, colon, plazaFrancia }, 3));

        var filtro = new LugarFiltroDto
        {
            LatitudReferencia = -34.6037m,
            LongitudReferencia = -58.3816m,
            RadioMetrosMaximo = 3000, // excluye el Rosedal (~4.8km)
            Pagina = 1,
            Tamano = 1
        };

        var primeraPagina = await _service.BuscarLugaresAsync(filtro);
        var itemsPrimera = primeraPagina.Items.ToList();

        // El total debe reflejar el filtro por radio (2, no 3) y la página el orden por cercanía.
        Assert.Equal(2, primeraPagina.Total);
        Assert.Equal(1, primeraPagina.Pagina);
        Assert.Equal(1, primeraPagina.Tamano);
        Assert.Single(itemsPrimera);
        Assert.Equal("Teatro Colón", itemsPrimera[0].Nombre);

        filtro.Pagina = 2;
        var segundaPagina = await _service.BuscarLugaresAsync(filtro);
        var itemsSegunda = segundaPagina.Items.ToList();

        Assert.Equal(2, segundaPagina.Total);
        Assert.Single(itemsSegunda);
        Assert.Equal("Plaza Francia", itemsSegunda[0].Nombre);
    }
}
