namespace Lugares.Domain.Entities;

public class ControlCuotaGoogle
{
    public DateOnly Fecha { get; set; }
    public int LlamadasRealizadas { get; set; } = 0;
    public int LimiteDiario { get; set; }
    public bool CircuitoAbierto { get; set; } = false;
    public DateTime FechaActualizacion { get; set; } = DateTime.UtcNow;
}
