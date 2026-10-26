using Lugares.Application.DTOs;
using Lugares.Domain.Entities;

namespace Lugares.Application.Interfaces;

public interface ILugarRepository
{
    Task<(IReadOnlyList<Lugar> Items, int Total)> ObtenerFiltradosAsync(LugarFiltroDto filtro, CancellationToken cancellationToken = default);
    Task<Lugar?> ObtenerPorIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<Lugar?> ObtenerPorGooglePlaceIdAsync(string googlePlaceId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<CategoriaLugar>> ObtenerCategoriasAsync(CancellationToken cancellationToken = default);
    Task AgregarAsync(Lugar lugar, CancellationToken cancellationToken = default);
    Task ActualizarAsync(Lugar lugar, CancellationToken cancellationToken = default);
    Task<bool> ExisteAsync(Guid id, CancellationToken cancellationToken = default);
}
