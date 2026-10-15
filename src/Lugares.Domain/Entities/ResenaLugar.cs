using Lugares.Domain.Enums;

namespace Lugares.Domain.Entities;

public class ResenaLugar
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid LugarId { get; set; }
    public Lugar Lugar { get; set; } = null!;

    public OrigenLugar Origen { get; set; } = OrigenLugar.CuradoLocal;
    public string? AutorNombre { get; set; }
    public short Puntuacion { get; set; }
    public string? Comentario { get; set; }
    public DateOnly? FechaResena { get; set; }
}
