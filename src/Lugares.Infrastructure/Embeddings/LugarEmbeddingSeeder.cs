
using Lugares.Application.Interfaces;
using Lugares.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace Lugares.Infrastructure.Embeddings
{
    public class LugarEmbeddingSeeder
    {
        private readonly ApplicationDbContext _context;
        private readonly ILugarEmbeddingTextBuilder _embeddingTextBuilder;
        private readonly ILugarEmbeddingHashBuilder _embeddingHashBuilder;
        private readonly IEmbeddingClient _embeddingClient;
        public LugarEmbeddingSeeder(
            ApplicationDbContext context, 
            ILugarEmbeddingTextBuilder embeddingTextBuilder, 
            ILugarEmbeddingHashBuilder embeddingHashBuilder,
            IEmbeddingClient embeddingClient)
        {
            _context = context;
            _embeddingTextBuilder = embeddingTextBuilder; 
            _embeddingHashBuilder = embeddingHashBuilder;
            _embeddingClient = embeddingClient;
        }
        public async Task GenerateMissingEmbeddingsAsync(CancellationToken cancellationToken = default)
        {
            var lugares = await _context.Lugares
                .Where(l => l.Embedding == null)
                .Include(l => l.Categorias)
                    .ThenInclude(lc => lc.Categoria)
                .Include(l => l.OpcionesDietarias)
                .ToListAsync(cancellationToken);

            foreach (var batch in lugares.Chunk(32))
            {
                var textos = batch
                    .Select(_embeddingTextBuilder.Build)
                    .ToList();

                var embeddings =
                    await _embeddingClient.GenerateDocumentEmbeddingsAsync(
                        textos,
                        cancellationToken);

                for (int i = 0; i < batch.Length; i++)
                {
                    batch[i].Embedding = new Pgvector.Vector(embeddings[i]);
                    batch[i].EmbeddingSourceHash = _embeddingHashBuilder.Build(batch[i]);
                    batch[i].EmbeddingUpdatedAt = DateTime.UtcNow;
                }

                await _context.SaveChangesAsync(cancellationToken);
            }
        }
    }
}
