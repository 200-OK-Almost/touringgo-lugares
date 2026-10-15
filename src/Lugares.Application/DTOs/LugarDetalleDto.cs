using Lugares.Domain.Enums;

namespace Lugares.Application.DTOs;

public class LugarDetalleDto
{
    public Guid Id { get; set; }
    public OrigenLugar Origen { get; set; }
    public string? IdGooglePlace { get; set; }
    public TipoLugar TipoLugar { get; set; }
    public string Nombre { get; set; } = string.Empty;
    public BarrioCaba Barrio { get; set; }
    public string? Subzona { get; set; }
    public string Direccion { get; set; } = string.Empty;
    public decimal Latitud { get; set; }
    public decimal Longitud { get; set; }
    public string? Descripcion { get; set; }
    public short DuracionEstimadaMinutos { get; set; }
    public short NivelPrecio { get; set; }
    public decimal? Puntuacion { get; set; }
    public int CantidadResenas { get; set; }
    public bool EsBajoTecho { get; set; }
    public bool EsAlAireLibre { get; set; }
    public bool AptoNinos { get; set; }
    public bool AccesibleMovilidadReducida { get; set; }
    public bool OfreceMenuInfantil { get; set; }
    public List<CategoriaDto> Categorias { get; set; } = new();
    public List<string> OpcionesDietarias { get; set; } = new();
    public List<FotoDto> Fotos { get; set; } = new();
    public List<ResenaDto> Resenas { get; set; } = new();
}
