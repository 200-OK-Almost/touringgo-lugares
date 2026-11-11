namespace Lugares.Infrastructure.Embeddings;

internal sealed class EmbeddingResponse
{
    public required List<List<float>> Embeddings { get; init; }

    public int Dimensions { get; init; }

    public int Count { get; init; }
}