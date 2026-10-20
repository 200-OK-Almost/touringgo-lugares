using Lugares.Application.DTOs;
using Lugares.Domain.Enums;

namespace Lugares.Application.Interfaces;

public interface IGooglePlacesService
{
    Task<IReadOnlyList<LugarResumenDto>> BuscarYCachearAsync(
        string busqueda,
        BarrioCaba barrio,
        CancellationToken cancellationToken = default);
}
