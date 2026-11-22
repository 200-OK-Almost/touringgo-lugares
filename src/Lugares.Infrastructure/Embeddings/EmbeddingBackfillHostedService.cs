using Lugares.Application.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Lugares.Infrastructure.Embeddings;

public class EmbeddingBackfillHostedService : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly EmbeddingBackfillOptions _options;
    private readonly ILogger<EmbeddingBackfillHostedService> _logger;

    public EmbeddingBackfillHostedService(
        IServiceScopeFactory scopeFactory,
        IOptions<EmbeddingBackfillOptions> options,
        ILogger<EmbeddingBackfillHostedService> logger)
    {
        _scopeFactory = scopeFactory;
        _options = options.Value;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        if (!_options.Enabled)
        {
            _logger.LogInformation("Backfill de embeddings deshabilitado.");
            return;
        }

        var intervalo = TimeSpan.FromSeconds(Math.Max(1, _options.IntervalSeconds));
        var backoff = intervalo;

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                using var scope = _scopeFactory.CreateScope();
                var backfill = scope.ServiceProvider
                    .GetRequiredService<LugarEmbeddingBackfillService>();

                var procesados = await backfill.EjecutarAsync(stoppingToken);

                if (procesados > 0)
                {
                    _logger.LogInformation(
                        "Backfill de embeddings: {Cantidad} lugares procesados.",
                        procesados);
                }

                backoff = intervalo;
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception ex)
            {
                _logger.LogWarning(
                    ex,
                    "Backfill de embeddings falló. Reintentando en {Segundos} segundos.",
                    backoff.TotalSeconds);

                backoff = TimeSpan.FromSeconds(
                    Math.Min(backoff.TotalSeconds * 2, _options.MaxBackoffSeconds));
            }

            try
            {
                await Task.Delay(backoff, stoppingToken);
            }
            catch (OperationCanceledException)
            {
                break;
            }
        }
    }
}
