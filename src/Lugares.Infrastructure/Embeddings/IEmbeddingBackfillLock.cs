namespace Lugares.Infrastructure.Embeddings;

public interface IEmbeddingBackfillLock
{
    Task<IAsyncDisposable?> TryAcquireAsync(CancellationToken cancellationToken = default);
}
