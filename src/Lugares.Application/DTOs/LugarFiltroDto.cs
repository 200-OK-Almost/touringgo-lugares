using Lugares.Domain.Enums;

namespace Lugares.Application.DTOs;

public class LugarFiltroDto
{
    public BarrioCaba? Barrio { get; set; }
    public TipoLugar? TipoLugar { get; set; }
    public string? CategoriaCodigo { get; set; }
    public bool? SoloBajoTecho { get; set; }
    public bool? SoloAlAireLibre { get; set; }
    public bool? AptoNinos { get; set; }
    public bool? AccesibleMovilidadReducida { get; set; }
    public bool? OfreceMenuInfantil { get; set; }
    public RestriccionDietaria? RestriccionDietaria { get; set; }
    public short? NivelPrecio { get; set; }
    public string? Busqueda { get; set; }

    // Parámetros opcionales para ordenar por cercanía
    public decimal? LatitudReferencia { get; set; }
    public decimal? LongitudReferencia { get; set; }
    public double? RadioMetrosMaximo { get; set; }
}
