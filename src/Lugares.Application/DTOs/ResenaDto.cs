namespace Lugares.Application.DTOs;

public class ResenaDto
{
    public Guid Id { get; set; }
    public string? AutorNombre { get; set; }
    public short Puntuacion { get; set; }
    public string? Comentario { get; set; }
    public DateOnly? FechaResena { get; set; }
}
