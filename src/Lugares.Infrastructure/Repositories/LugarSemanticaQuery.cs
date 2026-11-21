using Lugares.Application.DTOs;
using Lugares.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Pgvector;
using Pgvector.EntityFrameworkCore;

namespace Lugares.Infrastructure.Repositories;

public static class LugarSemanticaQuery
{
    public static IQueryable<LugarResultadoSemanticoDto> Construir(
        IQueryable<Lugar> lugares,
        Vector queryVector,
        LugarBusquedaSemanticaDto busqueda)
    {
        var cantidad = Math.Clamp(busqueda.Cantidad, 1, 100);

        var query = lugares
            .AsNoTracking()
            .Where(l => l.Activo && l.Embedding != null);

        query = LugarSemanticaFiltros.Aplicar(query, busqueda);

        var proyectada = query
            .Select(l => new
            {
                Lugar = l,
                Distancia = l.Embedding!.CosineDistance(queryVector)
            })
            .OrderBy(x => x.Distancia)
            .Take(cantidad);

        if (busqueda.SimilitudMinima.HasValue)
        {
            var minima = busqueda.SimilitudMinima.Value;
            proyectada = proyectada.Where(x => 1.0 - x.Distancia >= minima);
        }

        return proyectada.Select(x => new LugarResultadoSemanticoDto
        {
            Similitud = 1.0 - x.Distancia,
            Id = x.Lugar.Id,
            Origen = x.Lugar.Origen,
            IdGooglePlace = x.Lugar.IdGooglePlace,
            TipoLugar = x.Lugar.TipoLugar,
            Nombre = x.Lugar.Nombre,
            Barrio = x.Lugar.Barrio,
            Subzona = x.Lugar.Subzona,
            Direccion = x.Lugar.Direccion,
            Latitud = x.Lugar.Latitud,
            Longitud = x.Lugar.Longitud,
            Descripcion = x.Lugar.Descripcion,
            DuracionEstimadaMinutos = x.Lugar.DuracionEstimadaMinutos,
            NivelPrecio = x.Lugar.NivelPrecio,
            Puntuacion = x.Lugar.Puntuacion,
            CantidadResenas = x.Lugar.CantidadResenas,
            EsBajoTecho = x.Lugar.EsBajoTecho,
            EsAlAireLibre = x.Lugar.EsAlAireLibre,
            AptoNinos = x.Lugar.AptoNinos,
            AccesibleMovilidadReducida = x.Lugar.AccesibleMovilidadReducida,
            OfreceMenuInfantil = x.Lugar.OfreceMenuInfantil,
            Categorias = x.Lugar.Categorias.Select(c => new CategoriaDto
            {
                Id = c.Categoria.Id,
                Codigo = c.Categoria.Codigo,
                Nombre = c.Categoria.Nombre
            }).ToList(),
            OpcionesDietarias = x.Lugar.OpcionesDietarias
                .Select(o => o.Restriccion.ToString().ToLower())
                .ToList(),
            Fotos = x.Lugar.Fotos.OrderBy(f => f.Orden).Select(f => new FotoDto
            {
                Id = f.Id,
                Url = f.Url,
                Orden = f.Orden,
                EsPortada = f.EsPortada
            }).ToList(),
            Resenas = x.Lugar.Resenas
                .OrderByDescending(r => r.FechaResena)
                .Select(r => new ResenaDto
                {
                    Id = r.Id,
                    AutorNombre = r.AutorNombre,
                    Puntuacion = r.Puntuacion,
                    Comentario = r.Comentario,
                    FechaResena = r.FechaResena
                }).ToList()
        });
    }
}
