using System.Security.Cryptography;
using System.Text;
using Lugares.Application.Interfaces;
using Lugares.Domain.Entities;
using Microsoft.Extensions.Configuration;

namespace Lugares.Infrastructure.Embeddings;

public class LugarEmbeddingHashBuilder : ILugarEmbeddingHashBuilder
{
    public const string DefaultModel = "BAAI/bge-m3";
    public const string DefaultTextBuilderVersion = "v1";

    private readonly ILugarEmbeddingTextBuilder _textBuilder;
    private readonly string _model;
    private readonly string _textBuilderVersion;

    public LugarEmbeddingHashBuilder(
        ILugarEmbeddingTextBuilder textBuilder,
        IConfiguration configuration)
    {
        _textBuilder = textBuilder;
        _model = configuration["Embeddings:Model"] ?? DefaultModel;
        _textBuilderVersion = configuration["Embeddings:TextBuilderVersion"]
            ?? DefaultTextBuilderVersion;
    }

    public string Build(Lugar lugar)
    {
        var texto = _textBuilder.Build(lugar);
        var payload = $"{_model}\n{_textBuilderVersion}\n{texto}";
        var hash = SHA256.HashData(Encoding.UTF8.GetBytes(payload));

        return Convert.ToHexString(hash).ToLowerInvariant();
    }
}
