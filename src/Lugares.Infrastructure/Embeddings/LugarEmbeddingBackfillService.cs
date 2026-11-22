using Lugares.Application.Configuration;
using Lugares.Application.Interfaces;
using Lugares.Domain.Entities;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Pgvector;

namespace Lugares.Infrastructure.Embeddings;

public class LugarEmbeddingBackfillService
{
    private readonly ILugarRepository _lugarRepository;
    private readonly ILugarEmbeddingTextBuilder _textBuilder;
    private readonly ILugarEmbeddingHashBuilder _hashBuilder;
    private readonly IEmbeddingClient _embeddingClient;
    private readonly IEmbeddingBackfillLock _lock;
    private readonly EmbeddingBackfillOptions _options;
    private readonly ILogger<LugarEmbeddingBackfillService> _logger;

    public LugarEmbeddingBackfillService(
        ILugarRepository lugarRepository,
        ILugarEmbeddingTextBuilder textBuilder,
        ILugarEmbeddingHashBuilder hashBuilder,
        IEmbeddingClient embeddingClient,
        IEmbeddingBackfillLock lockService,
        IOptions<EmbeddingBackfillOptions> options,
        ILogger<LugarEmbeddingBackfillService> logger)
    {
        _lugarRepository = lugarRepository;
        _textBuilder = textBuilder;
        _hashBuilder = hashBuilder;
        _embeddingClient = embeddingClient;
        _lock = lockService;
        _options = options.Value;
        _logger = logger;
    }

    public async Task<int> EjecutarAsync(CancellationToken cancellationToken = default)
    {
        await using var lockHandle = await _lock.TryAcquireAsync(cancellationToken);
        if (lockHandle is null)
        {
            _logger.LogDebug("Backfill de embeddings omitido: otra instancia tiene el lock.");
            return 0;
        }

        var batchSize = Math.Clamp(_options.BatchSize, 1, 256);
        var lugares = await _lugarRepository.ObtenerCandidatosEmbeddingAsync(
            batchSize,
            cancellationToken);

        if (lugares.Count == 0)
        {
            return 0;
        }

        var sinHash = lugares
            .Where(l => l.Embedding != null && l.EmbeddingSourceHash == null)
            .ToList();

        var muestreoOk = sinHash.Count == 0
            || await MuestreoCoincideAsync(sinHash, cancellationToken);

        if (!muestreoOk)
        {
            _logger.LogWarning(
                "El muestreo de embeddings no coincide con el texto actual; " +
                "se re-embebedarán los lugares existentes sin hash.");
        }

        var paraEmbedder = new List<Lugar>();

        foreach (var lugar in lugares)
        {
            var hash = _hashBuilder.Build(lugar);

            if (lugar.Embedding != null && lugar.EmbeddingSourceHash == hash)
            {
                lugar.EmbeddingUpdatedAt = DateTime.UtcNow;
                continue;
            }

            if (lugar.Embedding != null && lugar.EmbeddingSourceHash == null && muestreoOk)
            {
                lugar.EmbeddingSourceHash = hash;
                lugar.EmbeddingUpdatedAt = DateTime.UtcNow;
                continue;
            }

            paraEmbedder.Add(lugar);
        }

        if (paraEmbedder.Count == 0)
        {
            await _lugarRepository.GuardarCambiosAsync(cancellationToken);
            return lugares.Count;
        }

        foreach (var batch in paraEmbedder.Chunk(batchSize))
        {
            var textos = batch.Select(_textBuilder.Build).ToList();

            var vectores = await _embeddingClient.GenerateDocumentEmbeddingsAsync(
                textos,
                cancellationToken);

            for (int i = 0; i < batch.Length; i++)
            {
                batch[i].Embedding = new Vector(vectores[i]);
                batch[i].EmbeddingSourceHash = _hashBuilder.Build(batch[i]);
                batch[i].EmbeddingUpdatedAt = DateTime.UtcNow;
            }

            await _lugarRepository.GuardarCambiosAsync(cancellationToken);
        }

        _logger.LogInformation(
            "Backfill de embeddings procesó {Cantidad} lugares ({Reembeddings} re-embeddings).",
            lugares.Count,
            paraEmbedder.Count);

        return lugares.Count;
    }

    private async Task<bool> MuestreoCoincideAsync(
        IReadOnlyCollection<Lugar> lugares,
        CancellationToken cancellationToken)
    {
        var sampleSize = Math.Clamp(_options.SampleSize, 1, lugares.Count);
        var muestra = lugares
            .OrderBy(_ => Random.Shared.Next())
            .Take(sampleSize)
            .ToList();

        var textos = muestra.Select(_textBuilder.Build).ToList();
        var vectores = await _embeddingClient.GenerateDocumentEmbeddingsAsync(
            textos,
            cancellationToken);

        for (int i = 0; i < muestra.Count; i++)
        {
            var similitud = SimilitudCoseno(vectores[i], muestra[i].Embedding!.ToArray());

            if (similitud < _options.SimilarityThreshold)
            {
                return false;
            }
        }

        return true;
    }

    private static double SimilitudCoseno(float[] a, float[] b)
    {
        if (a.Length != b.Length)
        {
            return 0;
        }

        double producto = 0;
        double normaA = 0;
        double normaB = 0;

        for (int i = 0; i < a.Length; i++)
        {
            producto += a[i] * b[i];
            normaA += a[i] * a[i];
            normaB += b[i] * b[i];
        }

        if (normaA == 0 || normaB == 0)
        {
            return 0;
        }

        return producto / (Math.Sqrt(normaA) * Math.Sqrt(normaB));
    }
}
