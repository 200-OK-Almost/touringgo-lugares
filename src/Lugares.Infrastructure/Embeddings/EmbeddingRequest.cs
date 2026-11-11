namespace Lugares.Infrastructure.Embeddings;

internal sealed class EmbeddingRequest
{
    public required List<string> Texts { get; init; }
}