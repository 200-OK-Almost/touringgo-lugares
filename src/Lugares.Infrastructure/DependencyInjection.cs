using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Lugares.Application.Interfaces;
using Lugares.Infrastructure.Data;
using Lugares.Infrastructure.Repositories;
using Lugares.Infrastructure.Services;

namespace Lugares.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.AddDbContext<ApplicationDbContext>(options =>
            options.UseNpgsql(
                configuration.GetConnectionString("DefaultConnection"),
                npgsqlOptions =>
                {
                    npgsqlOptions.UseNetTopologySuite();
                    npgsqlOptions.UseVector();
                }
            )
        );

        // Registrar repositorios
        services.AddScoped<ILugarRepository, LugarRepository>();

        // Registrar servicios de infraestructura
        services.AddScoped<IControlCuotaService, ControlCuotaService>();
        services.AddHttpClient<IGooglePlacesService, GooglePlacesService>();

        return services;
    }
}
