using Lugares.Application.DTOs;

namespace Lugares.Application.Interfaces;

public interface ILugarService
{
    Task<ResultadoPaginadoDto<LugarResumenDto>> BuscarLugaresAsync(LugarFiltroDto filtro, CancellationToken cancellationToken = default);
    Task<LugarDetalleDto?> ObtenerDetalleLugarAsync(Guid id, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<CategoriaDto>> ObtenerCategoriasAsync(CancellationToken cancellationToken = default);
}
