using Lugares.Application.Interfaces;
using Lugares.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Lugares.Infrastructure.Embeddings
{
    public class LugarEmbeddingSeeder
    {
        private static readonly SemaphoreSlim _lock = new(1, 1);

        private readonly ApplicationDbContext _context;
        private readonly ILugarEmbeddingTextBuilder _embeddingTextBuilder;
        private readonly IEmbeddingClient _embeddingClient;
        private readonly ILogger<LugarEmbeddingSeeder> _logger;

        public LugarEmbeddingSeeder(
            ApplicationDbContext context,
            ILugarEmbeddingTextBuilder embeddingTextBuilder,
            IEmbeddingClient embeddingClient,
            ILogger<LugarEmbeddingSeeder> logger)
        {
            _context = context;
            _embeddingTextBuilder = embeddingTextBuilder;
            _embeddingClient = embeddingClient;
            _logger = logger;
        }

        public async Task<int> GenerateMissingEmbeddingsAsync(CancellationToken cancellationToken = default)
        {
            if (!await _lock.WaitAsync(0, cancellationToken))
            {
                _logger.LogInformation("La generación de embeddings ya se encuentra en ejecución en otro hilo.");
                return 0;
            }

            try
            {
                var lugares = await _context.Lugares
                    .Where(l => l.Embedding == null)
                    .Include(l => l.Categorias)
                        .ThenInclude(lc => lc.Categoria)
                    .Include(l => l.OpcionesDietarias)
                    .ToListAsync(cancellationToken);

                if (lugares.Count == 0)
                {
                    return 0;
                }

                _logger.LogInformation(
                    "Iniciando generación automática de embeddings para {Total} lugares sin vector (en lotes de 32)...",
                    lugares.Count);

                var generados = 0;
                var batches = lugares.Chunk(32).ToList();

                for (int b = 0; b < batches.Count; b++)
                {
                    cancellationToken.ThrowIfCancellationRequested();
                    var batch = batches[b];

                    var textos = batch
                        .Select(_embeddingTextBuilder.Build)
                        .ToList();

                    try
                    {
                        var embeddings = await _embeddingClient.GenerateDocumentEmbeddingsAsync(
                            textos,
                            cancellationToken);

                        for (int i = 0; i < batch.Length; i++)
                        {
                            batch[i].Embedding = new Pgvector.Vector(embeddings[i]);
                        }

                        await _context.SaveChangesAsync(cancellationToken);
                        generados += batch.Length;

                        if ((b + 1) % 5 == 0 || b == batches.Count - 1)
                        {
                            _logger.LogInformation(
                                "Embeddings generados: {Generados}/{Total} lugares (lote {LoteActual}/{TotalLotes}).",
                                generados,
                                lugares.Count,
                                b + 1,
                                batches.Count);
                        }
                    }
                    catch (Exception ex) when (ex is not OperationCanceledException)
                    {
                        _logger.LogWarning(
                            ex,
                            "Error al generar embeddings en el lote {LoteActual}/{TotalLotes}. Se reintentará automáticamente.",
                            b + 1,
                            batches.Count);
                        break;
                    }
                }

                return generados;
            }
            finally
            {
                _lock.Release();
            }
        }
    }
}
