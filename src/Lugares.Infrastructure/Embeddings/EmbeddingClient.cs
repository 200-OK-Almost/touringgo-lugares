using System.Net.Http.Json;
using Lugares.Application.Interfaces;

namespace Lugares.Infrastructure.Embeddings;

internal sealed class EmbeddingClient : IEmbeddingClient
{
    private const int ExpectedDimensions = 384; // varia según modelo
    private const int MaxBatchSize = 32;

    private readonly HttpClient _httpClient;

    public EmbeddingClient(HttpClient httpClient)
    {
        _httpClient = httpClient;
    }

    public Task<IReadOnlyList<float[]>> GenerateDocumentEmbeddingsAsync(
        IReadOnlyList<string> texts,
        CancellationToken cancellationToken = default)
    {
        return GenerateAsync("embeddings/documents", texts,cancellationToken);
    }

    public Task<IReadOnlyList<float[]>> GenerateQueryEmbeddingsAsync(
        IReadOnlyList<string> texts,
        CancellationToken cancellationToken = default)
    {
        return GenerateAsync("embeddings/queries", texts, cancellationToken);
    }

    private async Task<IReadOnlyList<float[]>> GenerateAsync(
        string endpoint,
        IReadOnlyList<string> texts,
        CancellationToken cancellationToken)
    {
        if (texts.Count == 0 || texts.Count > MaxBatchSize)
        {
            throw new ArgumentException(
                $"La cantidad de textos debe estar entre 1 y {MaxBatchSize}.",
                nameof(texts));
        }

        if (texts.Any(string.IsNullOrWhiteSpace))
        {
            throw new ArgumentException(
                "Los textos no pueden estar vacíos.",
                nameof(texts));
        }

        var request = new EmbeddingRequest
        {
            Texts = texts.ToList()
        };

        using var response = await _httpClient.PostAsJsonAsync(
            endpoint,
            request,
            cancellationToken);

        // Propagar errores HTTP como excepciones
        response.EnsureSuccessStatusCode();

        var result = await response.Content.ReadFromJsonAsync<EmbeddingResponse>(
            cancellationToken: cancellationToken);

        if (result is null)
        {
            throw new InvalidOperationException("El servicio de embeddings devolvió una respuesta vacía.");
        }

        if (result.Count != texts.Count ||
            result.Embeddings.Count != texts.Count)
        {
            throw new InvalidOperationException("La cantidad de embeddings recibidos no coincide con la cantidad de textos enviados.");
        }

        if (result.Dimensions != ExpectedDimensions ||
            result.Embeddings.Any(e => e.Count != ExpectedDimensions))
        {
            throw new InvalidOperationException($"Se esperaban embeddings de {ExpectedDimensions} dimensiones.");
        }

        if (result.Embeddings.Any(
            e => e.Any(value => !float.IsFinite(value))))
        {
            throw new InvalidOperationException("El servicio devolvió embeddings con valores no finitos.");
        }

        return result.Embeddings
            .Select(e => e.ToArray())
            .ToList();
    }
}