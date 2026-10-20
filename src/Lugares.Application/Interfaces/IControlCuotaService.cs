using Lugares.Application.DTOs;

namespace Lugares.Application.Interfaces;

public interface IControlCuotaService
{
    Task<bool> PuedeRealizarLlamadaAsync(CancellationToken cancellationToken = default);
    Task RegistrarLlamadaAsync(CancellationToken cancellationToken = default);
    Task<ControlCuotaGoogleDto> ObtenerEstadoHoyAsync(CancellationToken cancellationToken = default);
}
