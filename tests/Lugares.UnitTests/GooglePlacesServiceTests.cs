using System.Net;
using System.Text;
using Lugares.Application.Interfaces;
using Lugares.Domain.Entities;
using Lugares.Domain.Enums;
using Lugares.Infrastructure.Services;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace Lugares.UnitTests;

public class GooglePlacesServiceTests
{
    private const string RespuestaGoogleDosLugares = """
        {
          "places": [
            {
              "id": "place_1",
              "displayName": { "text": "Café Lattente" },
              "formattedAddress": "Thames 1891, Buenos Aires",
              "location": { "latitude": -34.5885, "longitude": -58.4282 },
              "rating": 4.7,
              "userRatingCount": 1250,
              "priceLevel": "PRICE_LEVEL_MODERATE",
              "types": ["cafe"],
              "photos": []
            },
            {
              "id": "place_2",
              "displayName": { "text": "Museo de Arte" },
              "formattedAddress": "Av. San Juan 350, Buenos Aires",
              "location": { "latitude": -34.6225, "longitude": -58.3705 },
              "rating": 4.8,
              "userRatingCount": 900,
              "priceLevel": "PRICE_LEVEL_INEXPENSIVE",
              "types": ["museum"],
              "photos": []
            }
          ]
        }
        """;

    private const string RespuestaGoogleUnLugar = """
        {
          "places": [
            {
              "id": "place_1",
              "displayName": { "text": "Café Lattente" },
              "formattedAddress": "Thames 1891, Buenos Aires",
              "location": { "latitude": -34.5885, "longitude": -58.4282 },
              "rating": 4.7,
              "userRatingCount": 1250,
              "priceLevel": "PRICE_LEVEL_MODERATE",
              "types": ["cafe"],
              "photos": []
            }
          ]
        }
        """;

    private sealed class FakeHttpMessageHandler : HttpMessageHandler
    {
        private readonly string _contenido;

        public FakeHttpMessageHandler(string contenido)
        {
            _contenido = contenido;
        }

        protected override Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken)
        {
            return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(_contenido, Encoding.UTF8, "application/json")
            });
        }
    }

    private static float[] Vector1024()
    {
        return Enumerable.Repeat(1.0f, 1024).ToArray();
    }

    private static Mock<IConfiguration> CrearConfiguracionConApiKey()
    {
        var configMock = new Mock<IConfiguration>();
        configMock.Setup(c => c["GooglePlaces:ApiKey"]).Returns("TEST_KEY");
        return configMock;
    }

    private static GooglePlacesService CrearServicio(
        string respuestaGoogle,
        Mock<ILugarRepository> repoMock,
        Mock<IEmbeddingClient> embeddingClientMock,
        Mock<ILugarEmbeddingTextBuilder>? textBuilderMock = null,
        Mock<ILugarEmbeddingHashBuilder>? hashBuilderMock = null)
    {
        textBuilderMock ??= new Mock<ILugarEmbeddingTextBuilder>();
        textBuilderMock.Setup(t => t.Build(It.IsAny<Lugar>())).Returns("texto de embedding");

        hashBuilderMock ??= new Mock<ILugarEmbeddingHashBuilder>();
        hashBuilderMock.Setup(h => h.Build(It.IsAny<Lugar>())).Returns("hash-test");

        var httpClient = new HttpClient(new FakeHttpMessageHandler(respuestaGoogle));

        var controlCuotaMock = new Mock<IControlCuotaService>();
        controlCuotaMock.Setup(c => c.PuedeRealizarLlamadaAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        var configMock = CrearConfiguracionConApiKey();
        var loggerMock = new Mock<ILogger<GooglePlacesService>>();

        return new GooglePlacesService(
            httpClient,
            repoMock.Object,
            controlCuotaMock.Object,
            configMock.Object,
            loggerMock.Object,
            textBuilderMock.Object,
            hashBuilderMock.Object,
            embeddingClientMock.Object);
    }

    private static Mock<ILugarRepository> CrearRepoConCategorias()
    {
        var repoMock = new Mock<ILugarRepository>();
        repoMock.Setup(r => r.ObtenerCategoriasAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<CategoriaLugar>
            {
                new() { Id = 1, Codigo = "cafe_especialidad", Nombre = "Café de especialidad" }
            });
        return repoMock;
    }

    [Fact]
    public async Task BuscarYCachearAsync_LugaresNuevos_GeneraEmbeddingAntesDePersistir()
    {
        var repoMock = CrearRepoConCategorias();
        repoMock.Setup(r => r.ObtenerPorGooglePlaceIdAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Lugar?)null);

        var lugaresAgregados = new List<Lugar>();
        repoMock.Setup(r => r.AgregarAsync(It.IsAny<Lugar>(), It.IsAny<CancellationToken>()))
            .Callback<Lugar, CancellationToken>((lugar, _) => lugaresAgregados.Add(lugar))
            .Returns(Task.CompletedTask);

        var embeddingClientMock = new Mock<IEmbeddingClient>();
        var textosRecibidos = new List<IReadOnlyList<string>>();
        embeddingClientMock
            .Setup(c => c.GenerateDocumentEmbeddingsAsync(It.IsAny<IReadOnlyList<string>>(), It.IsAny<CancellationToken>()))
            .Callback<IReadOnlyList<string>, CancellationToken>((textos, _) => textosRecibidos.Add(textos))
            .ReturnsAsync((IReadOnlyList<string> textos, CancellationToken _) =>
                textos.Select(_ => Vector1024()).ToList());

        var service = CrearServicio(RespuestaGoogleDosLugares, repoMock, embeddingClientMock);

        var resultado = await service.BuscarYCachearAsync("cafe", BarrioCaba.Palermo);

        Assert.Equal(2, resultado.Count);
        Assert.Equal(2, lugaresAgregados.Count);
        Assert.All(lugaresAgregados, l => Assert.NotNull(l.Embedding));
        Assert.All(lugaresAgregados, l => Assert.Equal("hash-test", l.EmbeddingSourceHash));
        Assert.All(lugaresAgregados, l => Assert.NotNull(l.EmbeddingUpdatedAt));

        Assert.Single(textosRecibidos);
        Assert.Equal(2, textosRecibidos[0].Count);
    }

    [Fact]
    public async Task BuscarYCachearAsync_LugarExistenteSinEmbedding_BackfilleaVector()
    {
        var repoMock = CrearRepoConCategorias();

        var existente = new Lugar
        {
            Id = Guid.NewGuid(),
            Origen = OrigenLugar.GooglePlaces,
            IdGooglePlace = "place_1",
            Nombre = "Café Lattente",
            Barrio = BarrioCaba.Palermo,
            Direccion = "Thames 1891",
            Latitud = -34.5885m,
            Longitud = -58.4282m,
            Activo = true,
            Embedding = null
        };

        repoMock.Setup(r => r.ObtenerPorGooglePlaceIdAsync("place_1", It.IsAny<CancellationToken>()))
            .ReturnsAsync(existente);

        Lugar? actualizado = null;
        repoMock.Setup(r => r.ActualizarAsync(It.IsAny<Lugar>(), It.IsAny<CancellationToken>()))
            .Callback<Lugar, CancellationToken>((lugar, _) => actualizado = lugar)
            .Returns(Task.CompletedTask);

        var embeddingClientMock = new Mock<IEmbeddingClient>();
        embeddingClientMock
            .Setup(c => c.GenerateDocumentEmbeddingsAsync(It.IsAny<IReadOnlyList<string>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((IReadOnlyList<string> textos, CancellationToken _) =>
                textos.Select(_ => Vector1024()).ToList());

        var service = CrearServicio(RespuestaGoogleUnLugar, repoMock, embeddingClientMock);

        var resultado = await service.BuscarYCachearAsync("cafe", BarrioCaba.Palermo);

        Assert.Single(resultado);
        Assert.NotNull(actualizado);
        Assert.NotNull(actualizado.Embedding);
        Assert.Equal("hash-test", actualizado.EmbeddingSourceHash);
        Assert.NotNull(actualizado.EmbeddingUpdatedAt);
        repoMock.Verify(r => r.AgregarAsync(It.IsAny<Lugar>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task BuscarYCachearAsync_ServicioEmbeddingFalla_PersisteSinVectorYSinPerderResultados()
    {
        var repoMock = CrearRepoConCategorias();
        repoMock.Setup(r => r.ObtenerPorGooglePlaceIdAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Lugar?)null);

        var lugaresAgregados = new List<Lugar>();
        repoMock.Setup(r => r.AgregarAsync(It.IsAny<Lugar>(), It.IsAny<CancellationToken>()))
            .Callback<Lugar, CancellationToken>((lugar, _) => lugaresAgregados.Add(lugar))
            .Returns(Task.CompletedTask);

        var embeddingClientMock = new Mock<IEmbeddingClient>();
        embeddingClientMock
            .Setup(c => c.GenerateDocumentEmbeddingsAsync(It.IsAny<IReadOnlyList<string>>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("Servicio de embeddings no disponible"));

        var service = CrearServicio(RespuestaGoogleUnLugar, repoMock, embeddingClientMock);

        var resultado = await service.BuscarYCachearAsync("cafe", BarrioCaba.Palermo);

        Assert.Single(resultado);
        Assert.Single(lugaresAgregados);
        Assert.Null(lugaresAgregados[0].Embedding);
        Assert.Null(lugaresAgregados[0].EmbeddingSourceHash);
        Assert.Null(lugaresAgregados[0].EmbeddingUpdatedAt);
    }
}
