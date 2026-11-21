using Lugares.Application.DTOs;
using Lugares.Domain.Entities;

namespace Lugares.Infrastructure.Repositories;

public static class LugarSemanticaFiltros
{
    public static IQueryable<Lugar> Aplicar(
        IQueryable<Lugar> query,
        LugarBusquedaSemanticaDto busqueda)
    {
        if (busqueda.Barrio.HasValue)
        {
            query = query.Where(l => l.Barrio == busqueda.Barrio.Value);
        }

        if (busqueda.CategoriaCodigos is { Count: > 0 })
        {
            var codigos = busqueda.CategoriaCodigos
                .Where(c => !string.IsNullOrWhiteSpace(c))
                .ToList();

            if (codigos.Count > 0)
            {
                query = query.Where(l =>
                    l.Categorias.Any(c => codigos.Contains(c.Categoria.Codigo)));
            }
        }

        if (busqueda.NivelPrecioMaximo.HasValue)
        {
            query = query.Where(l => l.NivelPrecio <= busqueda.NivelPrecioMaximo.Value);
        }

        if (busqueda.SoloAlAireLibre == true)
        {
            query = query.Where(l => l.EsAlAireLibre);
        }

        if (busqueda.SoloBajoTecho == true)
        {
            query = query.Where(l => l.EsBajoTecho);
        }

        if (busqueda.AptoNinos == true)
        {
            query = query.Where(l => l.AptoNinos);
        }

        if (busqueda.AccesibleMovilidadReducida == true)
        {
            query = query.Where(l => l.AccesibleMovilidadReducida);
        }

        if (busqueda.RestriccionDietaria.HasValue)
        {
            query = query.Where(l =>
                l.OpcionesDietarias.Any(o => o.Restriccion == busqueda.RestriccionDietaria.Value));
        }

        if (busqueda.ExcluirIds is { Count: > 0 })
        {
            var ids = busqueda.ExcluirIds.ToList();
            query = query.Where(l => !ids.Contains(l.Id));
        }

        return query;
    }
}
