using Lugares.Application.Embeddings;
using Lugares.Application.Interfaces;
using Lugares.Application.Services;
using Microsoft.Extensions.DependencyInjection;

namespace Lugares.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplication(
        this IServiceCollection services)
    {
        services.AddScoped<ILugarService, LugarService>();

        // text builder de embedding
        services.AddSingleton<ILugarEmbeddingTextBuilder, BgeM3LugarEmbeddingTextBuilder>();
        return services;
    }
}