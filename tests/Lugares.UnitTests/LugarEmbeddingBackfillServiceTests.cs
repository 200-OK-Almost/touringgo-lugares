using Lugares.Application.Configuration;
using Lugares.Application.Interfaces;
using Lugares.Domain.Entities;
using Lugares.Infrastructure.Embeddings;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Moq;
using Pgvector;

namespace Lugares.UnitTests;

public class LugarEmbeddingBackfillServiceTests
{
    [Fact]
    public async Task EjecutarAsync_SinLock_NoProcesaNada()
    {
        var lugar = new Lugar { Id = Guid.NewGuid(), Nombre = "Café" };
        var repo = CrearRepo(lugar);
        var embeddings = CrearEmbeddings(new[] { 1f, 0f });
        var lockMock = new Mock<IEmbeddingBackfillLock>();
        lockMock
            .Setup(l => l.TryAcquireAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync((IAsyncDisposable?)null);

        var service = CrearServicio(repo, embeddings, lockMock: lockMock);

        var procesados = await service.EjecutarAsync();

        Assert.Equal(0, procesados);
        repo.Verify(
            r => r.ObtenerCandidatosEmbeddingAsync(
                It.IsAny<int>(),
                It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task EjecutarAsync_SinEmbedding_GeneraEmbeddingHashYFecha()
    {
        var lugar = new Lugar { Id = Guid.NewGuid(), Nombre = "Café" };
        var repo = CrearRepo(lugar);
        var embeddings = CrearEmbeddings(new[] { 1f, 0f });
        var service = CrearServicio(repo, embeddings);

        var procesados = await service.EjecutarAsync();

        Assert.Equal(1, procesados);
        Assert.NotNull(lugar.Embedding);
        Assert.Equal("hash:Café", lugar.EmbeddingSourceHash);
        Assert.NotNull(lugar.EmbeddingUpdatedAt);
        embeddings.Verify(
            c => c.GenerateDocumentEmbeddingsAsync(
                It.IsAny<IReadOnlyList<string>>(),
                It.IsAny<CancellationToken>()),
            Times.Once);
        repo.Verify(
            r => r.GuardarCambiosAsync(It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task EjecutarAsync_HashNuloYMuéstreoOk_NoReembebe()
    {
        var vector = new[] { 1f, 0f };
        var lugar = new Lugar
        {
            Id = Guid.NewGuid(),
            Nombre = "Café",
            Embedding = new Vector(vector),
            EmbeddingSourceHash = null
        };
        var repo = CrearRepo(lugar);
        var embeddings = CrearEmbeddings(vector);
        var service = CrearServicio(repo, embeddings);

        var procesados = await service.EjecutarAsync();

        Assert.Equal(1, procesados);
        Assert.Equal("hash:Café", lugar.EmbeddingSourceHash);
        Assert.NotNull(lugar.EmbeddingUpdatedAt);
        embeddings.Verify(
            c => c.GenerateDocumentEmbeddingsAsync(
                It.IsAny<IReadOnlyList<string>>(),
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task EjecutarAsync_HashNuloYMuéstreoFalla_Reembebe()
    {
        var viejo = new[] { 1f, 0f };
        var nuevo = new[] { 0f, 1f };
        var lugar = new Lugar
        {
            Id = Guid.NewGuid(),
            Nombre = "Café",
            Embedding = new Vector(viejo),
            EmbeddingSourceHash = null
        };
        var repo = CrearRepo(lugar);
        var embeddings = CrearEmbeddings(nuevo, nuevo);
        var service = CrearServicio(repo, embeddings);

        var procesados = await service.EjecutarAsync();

        Assert.Equal(1, procesados);
        Assert.Equal(nuevo, lugar.Embedding!.ToArray());
        Assert.Equal("hash:Café", lugar.EmbeddingSourceHash);
        Assert.NotNull(lugar.EmbeddingUpdatedAt);
        embeddings.Verify(
            c => c.GenerateDocumentEmbeddingsAsync(
                It.IsAny<IReadOnlyList<string>>(),
                It.IsAny<CancellationToken>()),
            Times.Exactly(2));
    }

    [Fact]
    public async Task EjecutarAsync_HashObsoleto_Reembebe()
    {
        var lugar = new Lugar
        {
            Id = Guid.NewGuid(),
            Nombre = "Café",
            Embedding = new Vector(new[] { 1f, 0f }),
            EmbeddingSourceHash = "hash-viejo"
        };
        var repo = CrearRepo(lugar);
        var embeddings = CrearEmbeddings(new[] { 0f, 1f });
        var service = CrearServicio(repo, embeddings);

        var procesados = await service.EjecutarAsync();

        Assert.Equal(1, procesados);
        Assert.Equal("hash:Café", lugar.EmbeddingSourceHash);
        Assert.NotNull(lugar.EmbeddingUpdatedAt);
        embeddings.Verify(
            c => c.GenerateDocumentEmbeddingsAsync(
                It.IsAny<IReadOnlyList<string>>(),
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task EjecutarAsync_HashAlDia_ActualizaFechaSinReembeber()
    {
        var viejaFecha = DateTime.UtcNow.AddDays(-1);
        var lugar = new Lugar
        {
            Id = Guid.NewGuid(),
            Nombre = "Café",
            Embedding = new Vector(new[] { 1f, 0f }),
            EmbeddingSourceHash = "hash:Café",
            EmbeddingUpdatedAt = viejaFecha,
            FechaActualizacion = DateTime.UtcNow
        };
        var repo = CrearRepo(lugar);
        var embeddings = CrearEmbeddings(new[] { 1f, 0f });
        var service = CrearServicio(repo, embeddings);

        var procesados = await service.EjecutarAsync();

        Assert.Equal(1, procesados);
        Assert.True(lugar.EmbeddingUpdatedAt > viejaFecha);
        embeddings.Verify(
            c => c.GenerateDocumentEmbeddingsAsync(
                It.IsAny<IReadOnlyList<string>>(),
                It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task EjecutarAsync_ProcesaEnLotes()
    {
        var lugares = Enumerable.Range(1, 3)
            .Select(i => new Lugar { Id = Guid.NewGuid(), Nombre = $"Lugar {i}" })
            .ToArray();
        var repo = CrearRepo(lugares);
        var embeddings = CrearEmbeddings(new[] { 1f, 0f });
        var service = CrearServicio(
            repo,
            embeddings,
            options: new EmbeddingBackfillOptions { BatchSize = 2 });

        var procesados = await service.EjecutarAsync();

        Assert.Equal(3, procesados);
        embeddings.Verify(
            c => c.GenerateDocumentEmbeddingsAsync(
                It.IsAny<IReadOnlyList<string>>(),
                It.IsAny<CancellationToken>()),
            Times.Exactly(2));
        repo.Verify(
            r => r.GuardarCambiosAsync(It.IsAny<CancellationToken>()),
            Times.Exactly(2));
    }

    private static Mock<ILugarRepository> CrearRepo(params Lugar[] lugares)
    {
        var repo = new Mock<ILugarRepository>();
        repo.Setup(r => r.ObtenerCandidatosEmbeddingAsync(
                It.IsAny<int>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(lugares);
        repo.Setup(r => r.GuardarCambiosAsync(It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        return repo;
    }

    private static Mock<IEmbeddingClient> CrearEmbeddings(params float[][] vectores)
    {
        var llamada = 0;
        var mock = new Mock<IEmbeddingClient>();

        mock.Setup(c => c.GenerateDocumentEmbeddingsAsync(
                It.IsAny<IReadOnlyList<string>>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync((IReadOnlyList<string> textos, CancellationToken _) =>
            {
                var vector = vectores[Math.Min(llamada, vectores.Length - 1)];
                llamada++;
                return textos.Select(_ => vector).ToList();
            });

        return mock;
    }

    private static LugarEmbeddingBackfillService CrearServicio(
        Mock<ILugarRepository> repo,
        Mock<IEmbeddingClient> embeddings,
        Mock<IEmbeddingBackfillLock>? lockMock = null,
        EmbeddingBackfillOptions? options = null)
    {
        var textBuilder = new Mock<ILugarEmbeddingTextBuilder>();
        textBuilder
            .Setup(t => t.Build(It.IsAny<Lugar>()))
            .Returns<Lugar>(l => $"texto:{l.Nombre}");

        var hashBuilder = new Mock<ILugarEmbeddingHashBuilder>();
        hashBuilder
            .Setup(h => h.Build(It.IsAny<Lugar>()))
            .Returns<Lugar>(l => $"hash:{l.Nombre}");

        if (lockMock is null)
        {
            lockMock = new Mock<IEmbeddingBackfillLock>();
            lockMock
                .Setup(l => l.TryAcquireAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync((IAsyncDisposable?)new FakeLockHandle());
        }

        return new LugarEmbeddingBackfillService(
            repo.Object,
            textBuilder.Object,
            hashBuilder.Object,
            embeddings.Object,
            lockMock.Object,
            Options.Create(options ?? new EmbeddingBackfillOptions()),
            NullLogger<LugarEmbeddingBackfillService>.Instance);
    }

    private sealed class FakeLockHandle : IAsyncDisposable
    {
        public ValueTask DisposeAsync() => ValueTask.CompletedTask;
    }
}
