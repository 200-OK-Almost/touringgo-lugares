using Lugares.Domain.Enums;

namespace Lugares.Application.DTOs
{
    public class LugarBusquedaSemanticaDto
    {
        public string Query { get; set; } = string.Empty;
        public int Cantidad { get; set; }
        public BarrioCaba? Barrio { get; set; }
        public IReadOnlyCollection<string>? CategoriaCodigos { get; set; }
        public short? NivelPrecioMaximo { get; set; }
        public bool? SoloAlAireLibre { get; set; }
        public bool? SoloBajoTecho { get; set; }
        public bool? AptoNinos { get; set; }
        public bool? AccesibleMovilidadReducida { get; set; }
        public RestriccionDietaria? RestriccionDietaria { get; set; }
        public IReadOnlyCollection<Guid>? ExcluirIds { get; set; }
        public decimal? LatitudReferencia { get; set; }
        public decimal? LongitudReferencia { get; set; }
        public double? RadioMetrosMaximo { get; set; }
    }
}
