using Lugares.Application.DTOs;
using Lugares.Application.Interfaces;
using Lugares.Domain.Entities;
using Lugares.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Pgvector;
using Pgvector.EntityFrameworkCore;

namespace Lugares.Infrastructure.Repositories;

public class LugarRepository : ILugarRepository
{
    private readonly ApplicationDbContext _context;

    public LugarRepository(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<(IReadOnlyList<Lugar> Items, int Total)> ObtenerFiltradosAsync(
        LugarFiltroDto filtro,
        CancellationToken cancellationToken = default)
    {
        var query = _context.Lugares
            .AsNoTracking()
            .Where(l => l.Activo)
            .Include(l => l.Categorias)
                .ThenInclude(lc => lc.Categoria)
            .Include(l => l.Fotos)
            .Include(l => l.OpcionesDietarias)
            .AsQueryable();

        if (filtro.Barrio.HasValue)
        {
            query = query.Where(l => l.Barrio == filtro.Barrio.Value);
        }

        if (filtro.TipoLugar.HasValue)
        {
            query = query.Where(l => l.TipoLugar == filtro.TipoLugar.Value);
        }

        if (filtro.CategoriaCodigo is { Count: > 0 })
        {
            var codigos = filtro.CategoriaCodigo
                .Where(c => !string.IsNullOrWhiteSpace(c))
                .ToList();

            if (codigos.Count > 0)
            {
                query = query.Where(l => l.Categorias.Any(c => codigos.Contains(c.Categoria.Codigo)));
            }
        }

        if (filtro.SoloBajoTecho == true)
        {
            query = query.Where(l => l.EsBajoTecho);
        }

        if (filtro.SoloAlAireLibre == true)
        {
            query = query.Where(l => l.EsAlAireLibre);
        }

        if (filtro.AptoNinos == true)
        {
            query = query.Where(l => l.AptoNinos);
        }

        if (filtro.AccesibleMovilidadReducida == true)
        {
            query = query.Where(l => l.AccesibleMovilidadReducida);
        }

        if (filtro.OfreceMenuInfantil == true)
        {
            query = query.Where(l => l.OfreceMenuInfantil);
        }

        if (filtro.RestriccionDietaria.HasValue)
        {
            query = query.Where(l => l.OpcionesDietarias.Any(o => o.Restriccion == filtro.RestriccionDietaria.Value));
        }

        if (filtro.NivelPrecio.HasValue)
        {
            query = query.Where(l => l.NivelPrecio <= filtro.NivelPrecio.Value);
        }

        if (!string.IsNullOrWhiteSpace(filtro.Busqueda))
        {
            var busqueda = filtro.Busqueda.Trim().ToLower();
            query = query.Where(l => l.Nombre.ToLower().Contains(busqueda) ||
                                     l.Direccion.ToLower().Contains(busqueda) ||
                                     (l.Descripcion != null && l.Descripcion.ToLower().Contains(busqueda)) ||
                                     (l.Subzona != null && l.Subzona.ToLower().Contains(busqueda)));
        }

        if (!string.IsNullOrWhiteSpace(filtro.Subzona))
        {
            var subzona = filtro.Subzona.Trim().ToLower();
            query = query.Where(l => l.Subzona != null && l.Subzona.ToLower().Contains(subzona));
        }

        if (filtro.ResenasMinimas is > 0)
        {
            query = query.Where(l => l.CantidadResenas >= filtro.ResenasMinimas.Value);
        }

        if (filtro.PuntuacionMinima.HasValue)
        {
            query = query.Where(l => l.Puntuacion >= filtro.PuntuacionMinima.Value);
        }

        var orden = filtro.Orden?.Trim().ToLowerInvariant();

        // Al ordenar por puntuación no tiene sentido incluir lugares sin rating:
        // en PostgreSQL un ORDER BY ... DESC coloca los NULL primero.
        if (orden == "puntuacion")
        {
            query = query.Where(l => l.Puntuacion != null);
        }

        var total = await query.CountAsync(cancellationToken);

        var pagina = filtro.Pagina < 1 ? 1 : filtro.Pagina;
        var tamano = Math.Clamp(filtro.Tamano, 1, 100);

        // Con coordenadas de referencia, la distancia se calcula en la capa de aplicación
        // sobre el conjunto completo (no paginado) para poder filtrar por radio y ordenar
        // por cercanía de forma correcta antes de paginar.
        var usaCercania = filtro.LatitudReferencia.HasValue && filtro.LongitudReferencia.HasValue;

        var ordenada = orden switch
        {
            "puntuacion" => query
                .OrderByDescending(l => l.Puntuacion)
                .ThenByDescending(l => l.CantidadResenas),
            "resenas" => query
                .OrderByDescending(l => l.CantidadResenas)
                .ThenByDescending(l => l.Puntuacion),
            _ => query.OrderBy(l => l.Nombre)
        };

        var items = usaCercania
            ? await ordenada.ToListAsync(cancellationToken)
            : await ordenada
                .Skip((pagina - 1) * tamano)
                .Take(tamano)
                .ToListAsync(cancellationToken);

        return (items, total);
    }

    public async Task<Lugar?> ObtenerPorIdAsync(
        Guid id,
        CancellationToken cancellationToken = default)
    {
        return await _context.Lugares
            .Include(l => l.Categorias)
                .ThenInclude(lc => lc.Categoria)
            .Include(l => l.Fotos)
            .Include(l => l.Resenas)
            .Include(l => l.OpcionesDietarias)
            .FirstOrDefaultAsync(l => l.Id == id, cancellationToken);
    }

    public async Task<Lugar?> ObtenerPorGooglePlaceIdAsync(
        string googlePlaceId,
        CancellationToken cancellationToken = default)
    {
        return await _context.Lugares
            .Include(l => l.Categorias)
                .ThenInclude(lc => lc.Categoria)
            .Include(l => l.Fotos)
            .Include(l => l.OpcionesDietarias)
            .FirstOrDefaultAsync(l => l.IdGooglePlace == googlePlaceId, cancellationToken);
    }

    public async Task<IReadOnlyList<CategoriaLugar>> ObtenerCategoriasAsync(
        CancellationToken cancellationToken = default)
    {
        return await _context.CategoriasLugar
            .OrderBy(c => c.Nombre)
            .ToListAsync(cancellationToken);
    }

    public async Task AgregarAsync(
        Lugar lugar,
        CancellationToken cancellationToken = default)
    {
        foreach (var lc in lugar.Categorias)
        {
            var trackedCategoria = _context.CategoriasLugar.Local.FirstOrDefault(c => c.Id == lc.CategoriaId);
            if (trackedCategoria != null)
            {
                lc.Categoria = trackedCategoria;
            }
            else if (lc.Categoria != null)
            {
                _context.Entry(lc.Categoria).State = EntityState.Unchanged;
            }
        }

        await _context.Lugares.AddAsync(lugar, cancellationToken);
        await _context.SaveChangesAsync(cancellationToken);
    }

    public async Task ActualizarAsync(
        Lugar lugar,
        CancellationToken cancellationToken = default)
    {
        foreach (var lc in lugar.Categorias)
        {
            var trackedCategoria = _context.CategoriasLugar.Local.FirstOrDefault(c => c.Id == lc.CategoriaId);
            if (trackedCategoria != null)
            {
                lc.Categoria = trackedCategoria;
            }
            else if (lc.Categoria != null)
            {
                _context.Entry(lc.Categoria).State = EntityState.Unchanged;
            }
        }

        lugar.FechaActualizacion = DateTime.UtcNow;
        _context.Lugares.Update(lugar);
        await _context.SaveChangesAsync(cancellationToken);
    }

    public async Task<bool> ExisteAsync(
        Guid id,
        CancellationToken cancellationToken = default)
    {
        return await _context.Lugares.AnyAsync(l => l.Id == id, cancellationToken);
    }

    public async Task<IReadOnlyList<Lugar>> ObtenerCandidatosEmbeddingAsync(
        int cantidad,
        CancellationToken cancellationToken = default)
        => await _context.Lugares
            .Where(l => l.Embedding == null
                || l.EmbeddingSourceHash == null
                || l.EmbeddingUpdatedAt == null
                || l.FechaActualizacion > l.EmbeddingUpdatedAt)
            .Include(l => l.Categorias)
                .ThenInclude(lc => lc.Categoria)
            .Include(l => l.OpcionesDietarias)
            .OrderBy(l => l.Id)
            .Take(cantidad)
            .ToListAsync(cancellationToken);

    public Task GuardarCambiosAsync(CancellationToken cancellationToken = default)
        => _context.SaveChangesAsync(cancellationToken);

    public async Task<IEnumerable<LugarResultadoSemanticoDto>> BusquedaSemanticaAsync(
        float[] queryEmbedding,
        LugarBusquedaSemanticaDto busqueda,
        CancellationToken cancellationToken = default)
    {
        var queryVector = new Vector(queryEmbedding);

        return await LugarSemanticaQuery
            .Construir(_context.Lugares, queryVector, busqueda)
            .ToListAsync(cancellationToken);
    }
}
