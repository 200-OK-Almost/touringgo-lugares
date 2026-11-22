using Lugares.Application.Interfaces;
using Lugares.Domain.Entities;
using Lugares.Infrastructure.Embeddings;
using Microsoft.Extensions.Configuration;
using Moq;

namespace Lugares.UnitTests;

public class LugarEmbeddingHashBuilderTests
{
    [Fact]
    public void Build_EsDeterministaYDevuelveSha256Hex()
    {
        var builder = CrearBuilder("texto de embedding");
        var lugar = new Lugar { Nombre = "Café" };

        var hash1 = builder.Build(lugar);
        var hash2 = builder.Build(lugar);

        Assert.Equal(hash1, hash2);
        Assert.Equal(64, hash1.Length);
        Assert.Matches("^[0-9a-f]{64}$", hash1);
    }

    [Fact]
    public void Build_CambiaSiCambiaElTextoFuente()
    {
        var builderA = CrearBuilder("texto A");
        var builderB = CrearBuilder("texto B");
        var lugar = new Lugar { Nombre = "Café" };

        Assert.NotEqual(builderA.Build(lugar), builderB.Build(lugar));
    }

    [Fact]
    public void Build_CambiaSiCambiaElModelo()
    {
        var builderA = CrearBuilder("texto", modelo: "modelo-a");
        var builderB = CrearBuilder("texto", modelo: "modelo-b");
        var lugar = new Lugar { Nombre = "Café" };

        Assert.NotEqual(builderA.Build(lugar), builderB.Build(lugar));
    }

    [Fact]
    public void Build_CambiaSiCambiaLaVersionDelBuilder()
    {
        var builderA = CrearBuilder("texto", version: "v1");
        var builderB = CrearBuilder("texto", version: "v2");
        var lugar = new Lugar { Nombre = "Café" };

        Assert.NotEqual(builderA.Build(lugar), builderB.Build(lugar));
    }

    private static LugarEmbeddingHashBuilder CrearBuilder(
        string texto,
        string? modelo = null,
        string? version = null)
    {
        var textBuilder = new Mock<ILugarEmbeddingTextBuilder>();
        textBuilder.Setup(t => t.Build(It.IsAny<Lugar>())).Returns(texto);

        var configuration = new Mock<IConfiguration>();
        configuration.Setup(c => c["Embeddings:Model"]).Returns(modelo);
        configuration.Setup(c => c["Embeddings:TextBuilderVersion"]).Returns(version);

        return new LugarEmbeddingHashBuilder(textBuilder.Object, configuration.Object);
    }
}
