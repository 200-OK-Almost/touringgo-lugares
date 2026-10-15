using Lugares.Domain.Enums;

namespace Lugares.Domain.Entities;

public class Lugar
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public OrigenLugar Origen { get; set; } = OrigenLugar.CuradoLocal;
    public string? IdGooglePlace { get; set; }
    public DateTime? FechaUltimaSincronizacion { get; set; }
    public TipoLugar TipoLugar { get; set; } = TipoLugar.PuntoInteres;
    public string Nombre { get; set; } = string.Empty;
    public BarrioCaba Barrio { get; set; }
    public string? Subzona { get; set; }
    public string Direccion { get; set; } = string.Empty;
    public decimal Latitud { get; set; }
    public decimal Longitud { get; set; }
    public string? Descripcion { get; set; }
    public short DuracionEstimadaMinutos { get; set; } = 60;
    public short NivelPrecio { get; set; } = 1; // 1: $, 2: $$, 3: $$$
    public decimal? Puntuacion { get; set; } // 1.0 a 5.0
    public int CantidadResenas { get; set; } = 0;

    // Modo Lluvia / indoor-outdoor
    public bool EsBajoTecho { get; set; } = true;
    public bool EsAlAireLibre { get; set; } = false;

    // Modo Familia / accesibilidad
    public bool AptoNinos { get; set; } = true;
    public bool AccesibleMovilidadReducida { get; set; } = false;
    public bool OfreceMenuInfantil { get; set; } = false;

    public bool Activo { get; set; } = true;
    public DateTime FechaCreacion { get; set; } = DateTime.UtcNow;
    public DateTime FechaActualizacion { get; set; } = DateTime.UtcNow;

    // Navegación
    public ICollection<LugarCategoria> Categorias { get; set; } = new List<LugarCategoria>();
    public ICollection<LugarOpcionDietaria> OpcionesDietarias { get; set; } = new List<LugarOpcionDietaria>();
    public ICollection<FotoLugar> Fotos { get; set; } = new List<FotoLugar>();
    public ICollection<ResenaLugar> Resenas { get; set; } = new List<ResenaLugar>();
}
