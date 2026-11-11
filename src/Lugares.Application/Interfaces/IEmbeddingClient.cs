namespace Lugares.Application.Interfaces
{
    public interface IEmbeddingClient
    {
        Task<IReadOnlyList<float[]>> GenerateDocumentEmbeddingsAsync(
            IReadOnlyList<string> texts,
            CancellationToken cancellationToken = default);

        Task<IReadOnlyList<float[]>> GenerateQueryEmbeddingsAsync(
            IReadOnlyList<string> texts,
            CancellationToken cancellationToken = default);
    }
}
