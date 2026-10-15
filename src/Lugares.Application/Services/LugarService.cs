using Lugares.Application.DTOs;
using Lugares.Application.Interfaces;
using Lugares.Domain.Entities;

namespace Lugares.Application.Services;

public class LugarService : ILugarService
{
    private readonly ILugarRepository _lugarRepository;

    public LugarService(ILugarRepository lugarRepository)
    {
        _lugarRepository = lugarRepository;
    }

    public async Task<IReadOnlyList<LugarResumenDto>> BuscarLugaresAsync(
        LugarFiltroDto filtro,
        CancellationToken cancellationToken = default)
    {
        var lugares = await _lugarRepository.ObtenerFiltradosAsync(filtro, cancellationToken);

        var resultado = lugares.Select(l =>
        {
            double? distancia = null;
            if (filtro.LatitudReferencia.HasValue && filtro.LongitudReferencia.HasValue)
            {
                distancia = Math.Round(CalcularHaversineMetros(
                    (double)filtro.LatitudReferencia.Value,
                    (double)filtro.LongitudReferencia.Value,
                    (double)l.Latitud,
                    (double)l.Longitud), 1);
            }

            return new LugarResumenDto
            {
                Id = l.Id,
                Nombre = l.Nombre,
                TipoLugar = l.TipoLugar,
                Barrio = l.Barrio,
                Subzona = l.Subzona,
                Direccion = l.Direccion,
                Latitud = l.Latitud,
                Longitud = l.Longitud,
                DuracionEstimadaMinutos = l.DuracionEstimadaMinutos,
                NivelPrecio = l.NivelPrecio,
                Puntuacion = l.Puntuacion,
                CantidadResenas = l.CantidadResenas,
                EsBajoTecho = l.EsBajoTecho,
                EsAlAireLibre = l.EsAlAireLibre,
                AptoNinos = l.AptoNinos,
                AccesibleMovilidadReducida = l.AccesibleMovilidadReducida,
                FotoPortadaUrl = l.Fotos.FirstOrDefault(f => f.EsPortada)?.Url ?? l.Fotos.OrderBy(f => f.Orden).FirstOrDefault()?.Url,
                Categorias = l.Categorias.Select(c => c.Categoria.Nombre).ToList(),
                DistanciaMetros = distancia
            };
        }).ToList();

        if (filtro.LatitudReferencia.HasValue && filtro.LongitudReferencia.HasValue)
        {
            if (filtro.RadioMetrosMaximo.HasValue)
            {
                resultado = resultado.Where(r => r.DistanciaMetros <= filtro.RadioMetrosMaximo.Value).ToList();
            }

            resultado = resultado.OrderBy(r => r.DistanciaMetros).ToList();
        }

        return resultado;
    }

    public async Task<LugarDetalleDto?> ObtenerDetalleLugarAsync(
        Guid id,
        CancellationToken cancellationToken = default)
    {
        var lugar = await _lugarRepository.ObtenerPorIdAsync(id, cancellationToken);
        if (lugar == null) return null;

        return new LugarDetalleDto
        {
            Id = lugar.Id,
            Origen = lugar.Origen,
            IdGooglePlace = lugar.IdGooglePlace,
            TipoLugar = lugar.TipoLugar,
            Nombre = lugar.Nombre,
            Barrio = lugar.Barrio,
            Subzona = lugar.Subzona,
            Direccion = lugar.Direccion,
            Latitud = lugar.Latitud,
            Longitud = lugar.Longitud,
            Descripcion = lugar.Descripcion,
            DuracionEstimadaMinutos = lugar.DuracionEstimadaMinutos,
            NivelPrecio = lugar.NivelPrecio,
            Puntuacion = lugar.Puntuacion,
            CantidadResenas = lugar.CantidadResenas,
            EsBajoTecho = lugar.EsBajoTecho,
            EsAlAireLibre = lugar.EsAlAireLibre,
            AptoNinos = lugar.AptoNinos,
            AccesibleMovilidadReducida = lugar.AccesibleMovilidadReducida,
            OfreceMenuInfantil = lugar.OfreceMenuInfantil,
            Categorias = lugar.Categorias.Select(c => new CategoriaDto
            {
                Id = c.Categoria.Id,
                Codigo = c.Categoria.Codigo,
                Nombre = c.Categoria.Nombre
            }).ToList(),
            OpcionesDietarias = lugar.OpcionesDietarias.Select(o => o.Restriccion.ToString().ToLower()).ToList(),
            Fotos = lugar.Fotos.OrderBy(f => f.Orden).Select(f => new FotoDto
            {
                Id = f.Id,
                Url = f.Url,
                Orden = f.Orden,
                EsPortada = f.EsPortada
            }).ToList(),
            Resenas = lugar.Resenas.OrderByDescending(r => r.FechaResena).Select(r => new ResenaDto
            {
                Id = r.Id,
                AutorNombre = r.AutorNombre,
                Puntuacion = r.Puntuacion,
                Comentario = r.Comentario,
                FechaResena = r.FechaResena
            }).ToList()
        };
    }

    public async Task<IReadOnlyList<CategoriaDto>> ObtenerCategoriasAsync(
        CancellationToken cancellationToken = default)
    {
        var categorias = await _lugarRepository.ObtenerCategoriasAsync(cancellationToken);
        return categorias.Select(c => new CategoriaDto
        {
            Id = c.Id,
            Codigo = c.Codigo,
            Nombre = c.Nombre
        }).ToList();
    }

    private static double CalcularHaversineMetros(double lat1, double lon1, double lat2, double lon2)
    {
        const double radioTierra = 6371000;
        var dLat = (lat2 - lat1) * Math.PI / 180.0;
        var dLon = (lon2 - lon1) * Math.PI / 180.0;

        var a = Math.Sin(dLat / 2) * Math.Sin(dLat / 2) +
                Math.Cos(lat1 * Math.PI / 180.0) * Math.Cos(lat2 * Math.PI / 180.0) *
                Math.Sin(dLon / 2) * Math.Sin(dLon / 2);

        var c = 2 * Math.Atan2(Math.Sqrt(a), Math.Sqrt(1 - a));
        return radioTierra * c;
    }
}
