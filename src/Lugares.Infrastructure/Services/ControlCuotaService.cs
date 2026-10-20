using Lugares.Application.DTOs;
using Lugares.Application.Interfaces;
using Lugares.Domain.Entities;
using Lugares.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace Lugares.Infrastructure.Services;

public class ControlCuotaService : IControlCuotaService
{
    private readonly ApplicationDbContext _context;
    private readonly IConfiguration _configuration;
    private readonly ILogger<ControlCuotaService> _logger;

    public ControlCuotaService(
        ApplicationDbContext context,
        IConfiguration configuration,
        ILogger<ControlCuotaService> logger)
    {
        _context = context;
        _configuration = configuration;
        _logger = logger;
    }

    public async Task<bool> PuedeRealizarLlamadaAsync(CancellationToken cancellationToken = default)
    {
        var cuotaHoy = await ObtenerOCrearCuotaHoyAsync(cancellationToken);

        if (cuotaHoy.CircuitoAbierto)
        {
            _logger.LogWarning("Circuito abierto: se ha alcanzado el límite diario de llamadas a Google Places ({Limite}).", cuotaHoy.LimiteDiario);
            return false;
        }

        if (cuotaHoy.LlamadasRealizadas >= cuotaHoy.LimiteDiario)
        {
            cuotaHoy.CircuitoAbierto = true;
            cuotaHoy.FechaActualizacion = DateTime.UtcNow;
            await _context.SaveChangesAsync(cancellationToken);

            _logger.LogWarning("Límite diario alcanzado ({Llamadas}/{Limite}). Abriendo circuito para proteger la cuota.",
                cuotaHoy.LlamadasRealizadas, cuotaHoy.LimiteDiario);
            return false;
        }

        return true;
    }

    public async Task RegistrarLlamadaAsync(CancellationToken cancellationToken = default)
    {
        var cuotaHoy = await ObtenerOCrearCuotaHoyAsync(cancellationToken);

        cuotaHoy.LlamadasRealizadas++;
        cuotaHoy.FechaActualizacion = DateTime.UtcNow;

        if (cuotaHoy.LlamadasRealizadas >= cuotaHoy.LimiteDiario)
        {
            cuotaHoy.CircuitoAbierto = true;
            _logger.LogWarning("Se alcanzó el límite diario tras registrar la llamada. Circuito abierto activado.");
        }

        await _context.SaveChangesAsync(cancellationToken);
    }

    public async Task<ControlCuotaGoogleDto> ObtenerEstadoHoyAsync(CancellationToken cancellationToken = default)
    {
        var cuotaHoy = await ObtenerOCrearCuotaHoyAsync(cancellationToken);

        return new ControlCuotaGoogleDto
        {
            Fecha = cuotaHoy.Fecha,
            LlamadasRealizadas = cuotaHoy.LlamadasRealizadas,
            LimiteDiario = cuotaHoy.LimiteDiario,
            CircuitoAbierto = cuotaHoy.CircuitoAbierto,
            FechaActualizacion = cuotaHoy.FechaActualizacion
        };
    }

    private async Task<ControlCuotaGoogle> ObtenerOCrearCuotaHoyAsync(CancellationToken cancellationToken)
    {
        var hoy = DateOnly.FromDateTime(DateTime.UtcNow);
        var cuota = await _context.ControlCuotaGoogle.FirstOrDefaultAsync(c => c.Fecha == hoy, cancellationToken);

        if (cuota == null)
        {
            var limiteStr = _configuration["GooglePlaces:LimiteDiario"];
            var limiteConfigurado = int.TryParse(limiteStr, out var lim) ? lim : 100;

            cuota = new ControlCuotaGoogle
            {
                Fecha = hoy,
                LlamadasRealizadas = 0,
                LimiteDiario = limiteConfigurado,
                CircuitoAbierto = false,
                FechaActualizacion = DateTime.UtcNow
            };

            await _context.ControlCuotaGoogle.AddAsync(cuota, cancellationToken);
            await _context.SaveChangesAsync(cancellationToken);
        }

        return cuota;
    }
}
