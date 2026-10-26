using Lugares.Application.DTOs;
using Lugares.Application.Interfaces;
using Lugares.Domain.Entities;
using Lugares.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

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

        if (!string.IsNullOrWhiteSpace(filtro.CategoriaCodigo))
        {
            query = query.Where(l => l.Categorias.Any(c => c.Categoria.Codigo == filtro.CategoriaCodigo));
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
                                     (l.Descripcion != null && l.Descripcion.ToLower().Contains(busqueda)) ||
                                     (l.Subzona != null && l.Subzona.ToLower().Contains(busqueda)));
        }

        var total = await query.CountAsync(cancellationToken);

        var pagina = filtro.Pagina < 1 ? 1 : filtro.Pagina;
        var tamano = Math.Clamp(filtro.Tamano, 1, 100);

        var items = await query
            .OrderBy(l => l.Nombre)
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
            .Include(l => l.Fotos)
            .FirstOrDefaultAsync(l => l.IdGooglePlace == googlePlaceId, cancellationToken);
    }

    public async Task<IReadOnlyList<CategoriaLugar>> ObtenerCategoriasAsync(
        CancellationToken cancellationToken = default)
    {
        return await _context.CategoriasLugar
            .AsNoTracking()
            .OrderBy(c => c.Nombre)
            .ToListAsync(cancellationToken);
    }

    public async Task AgregarAsync(
        Lugar lugar,
        CancellationToken cancellationToken = default)
    {
        await _context.Lugares.AddAsync(lugar, cancellationToken);
        await _context.SaveChangesAsync(cancellationToken);
    }

    public async Task ActualizarAsync(
        Lugar lugar,
        CancellationToken cancellationToken = default)
    {
        _context.Lugares.Update(lugar);
        await _context.SaveChangesAsync(cancellationToken);
    }

    public async Task<bool> ExisteAsync(
        Guid id,
        CancellationToken cancellationToken = default)
    {
        return await _context.Lugares.AnyAsync(l => l.Id == id, cancellationToken);
    }
}
