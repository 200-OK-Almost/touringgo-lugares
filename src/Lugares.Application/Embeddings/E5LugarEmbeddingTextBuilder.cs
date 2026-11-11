using Lugares.Application.Interfaces;
using Lugares.Domain.Entities;
using Lugares.Domain.Enums;

namespace Lugares.Application.Embeddings
{
    public class E5LugarEmbeddingTextBuilder : ILugarEmbeddingTextBuilder
    {
        public string Build(Lugar lugar)
        {
            var partes = new List<string>()
            {
                $"Nombre: {lugar.Nombre}",
                $"Tipo de lugar: {GetNombreTipoLugar(lugar.TipoLugar)}",
                $"Categorías: {string.Join(", ", lugar.Categorias.Select(c => c.Categoria.Nombre))}",
                $"Barrio: {lugar.Barrio}",
                $"Subzona: {lugar.Subzona}",
            };

            if (!string.IsNullOrWhiteSpace(lugar.Descripcion))
                partes.Add($"Descripción: {lugar.Descripcion}");

            var caracteristicas = new List<string>();

            if (lugar.EsAlAireLibre)
                caracteristicas.Add("Aire Libre");

            if (lugar.EsBajoTecho)
                caracteristicas.Add("Bajo techo");

            caracteristicas.Add(lugar.AptoNinos ? "Apto para niños" : "No apto para niños");

            caracteristicas.Add(
                lugar.AccesibleMovilidadReducida
                    ? "Accesible para movilidad reducida"
                    : "No accesible para movilidad reducida");

            caracteristicas.Add(lugar.OfreceMenuInfantil ? "Ofrece menu infantil" : "Sin menu infantil");

            if (caracteristicas.Count > 0)
                partes.Add($"Características: {string.Join(", ", caracteristicas)}");

            if (lugar.OpcionesDietarias.Count > 0)
            {
                partes.Add($"Opciones dietarias: {string.Join(
                    ", ", lugar.OpcionesDietarias.Select(o => GetNombreOpcionDietaria(o.Restriccion)))}");
            }

            return string.Join("\n", partes);
        }
        private static string GetNombreTipoLugar(TipoLugar tipo)
        {
            return tipo switch
            {
                TipoLugar.PuntoInteres => "Punto de interés turístico",
                TipoLugar.Alojamiento => "Alojamiento turístico",
                TipoLugar.NodoUrbano => "Nodo urbano",
                _ => tipo.ToString()
            };
        }
        private static string GetNombreOpcionDietaria(RestriccionDietaria opcion)
        {
            return opcion switch
            {
                RestriccionDietaria.Celiaco => "Para celiacos",
                RestriccionDietaria.Vegano => "Para veganos",
                RestriccionDietaria.Vegetariano => "Para vegetarianos",
                RestriccionDietaria.IntoleranteLactosa => "Para Intolerantes a la lactosa",
                _ => opcion.ToString()
            };
        }
    }
}
