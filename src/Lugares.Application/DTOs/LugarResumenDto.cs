using Lugares.Domain.Enums;

namespace Lugares.Application.DTOs;

public class LugarResumenDto
{
    public Guid Id { get; set; }
    public string Nombre { get; set; } = string.Empty;
    public TipoLugar TipoLugar { get; set; }
    public BarrioCaba Barrio { get; set; }
    public string? Subzona { get; set; }
    public string Direccion { get; set; } = string.Empty;
    public decimal Latitud { get; set; }
    public decimal Longitud { get; set; }
    public short DuracionEstimadaMinutos { get; set; }
    public short NivelPrecio { get; set; }
    public decimal? Puntuacion { get; set; }
    public int CantidadResenas { get; set; }
    public bool EsBajoTecho { get; set; }
    public bool EsAlAireLibre { get; set; }
    public bool AptoNinos { get; set; }
    public bool AccesibleMovilidadReducida { get; set; }
    public string? FotoPortadaUrl { get; set; }
    public List<string> Categorias { get; set; } = new();
    public double? DistanciaMetros { get; set; }
}
