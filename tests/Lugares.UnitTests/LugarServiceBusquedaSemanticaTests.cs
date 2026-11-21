using Lugares.Application.DTOs;
using Lugares.Application.Interfaces;
using Lugares.Application.Services;
using Moq;

namespace Lugares.UnitTests;

public class LugarServiceBusquedaSemanticaTests
{
    private readonly Mock<ILugarRepository> _repoMock;
    private readonly Mock<IEmbeddingClient> _embeddingClientMock;
    private readonly LugarService _service;

    public LugarServiceBusquedaSemanticaTests()
    {
        _repoMock = new Mock<ILugarRepository>();
        _embeddingClientMock = new Mock<IEmbeddingClient>();
        _service = new LugarService(_repoMock.Object, _embeddingClientMock.Object);
    }

    [Fact]
    public async Task BusquedaSemanticaAsync_UsaLaQueryYPropagaLosFiltros()
    {
        var busqueda = new LugarBusquedaSemanticaDto
        {
            Query = "café de especialidad",
            Cantidad = 5,
            Barrio = Lugares.Domain.Enums.BarrioCaba.Palermo,
            SoloAlAireLibre = true
        };

        var embedding = new float[] { 0.1f, 0.2f };
        var resultado = new List<LugarResultadoSemanticoDto>
        {
            new() { Nombre = "Café Palermo" }
        };

        _embeddingClientMock
            .Setup(c => c.GenerateQueryEmbeddingsAsync(
                It.IsAny<IReadOnlyList<string>>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<float[]> { embedding });

        _repoMock
            .Setup(r => r.BusquedaSemanticaAsync(
                embedding,
                busqueda,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(resultado);

        var items = await _service.BusquedaSemanticaAsync(busqueda);

        Assert.Same(resultado, items);
        _embeddingClientMock.Verify(c => c.GenerateQueryEmbeddingsAsync(
            It.Is<IReadOnlyList<string>>(textos =>
                textos.Count == 1 && textos[0] == busqueda.Query),
            It.IsAny<CancellationToken>()), Times.Once);
        _repoMock.Verify(r => r.BusquedaSemanticaAsync(
            embedding,
            busqueda,
            It.IsAny<CancellationToken>()), Times.Once);
    }
}
