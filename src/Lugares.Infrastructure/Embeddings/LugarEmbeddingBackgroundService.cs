using Lugares.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Lugares.Infrastructure.Embeddings;

public class LugarEmbeddingBackgroundService : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<LugarEmbeddingBackgroundService> _logger;

    public LugarEmbeddingBackgroundService(
        IServiceScopeFactory scopeFactory,
        ILogger<LugarEmbeddingBackgroundService> logger)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        // Pequeña espera inicial para permitir que las migraciones y el seed de arranque finalicen
        await Task.Delay(TimeSpan.FromSeconds(3), stoppingToken);

        while (!stoppingToken.IsCancellationRequested)
        {
            var pendientesRestantes = 0;

            try
            {
                using var scope = _scopeFactory.CreateScope();
                var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

                var pendientesAntes = await dbContext.Lugares
                    .CountAsync(l => l.Embedding == null, stoppingToken);

                if (pendientesAntes > 0)
                {
                    var seeder = scope.ServiceProvider.GetRequiredService<LugarEmbeddingSeeder>();
                    await seeder.GenerateMissingEmbeddingsAsync(stoppingToken);

                    pendientesRestantes = await dbContext.Lugares
                        .CountAsync(l => l.Embedding == null, stoppingToken);
                }
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                _logger.LogWarning(ex, "El worker de embeddings encontró un error temporal; reintentará en breve.");
                pendientesRestantes = 1;
            }

            // Si aún quedan lugares sin embedding (ej. servicio Python iniciando), reintentar en 15s;
            // de lo contrario, monitorear cada 60s por si se insertan nuevos lugares sin vector.
            var delay = pendientesRestantes > 0
                ? TimeSpan.FromSeconds(15)
                : TimeSpan.FromSeconds(60);

            await Task.Delay(delay, stoppingToken);
        }
    }
}
