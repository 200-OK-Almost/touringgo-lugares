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

        return services;
    }
}