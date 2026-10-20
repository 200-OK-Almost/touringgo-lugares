namespace Lugares.Application.DTOs;

public class ControlCuotaGoogleDto
{
    public DateOnly Fecha { get; set; }
    public int LlamadasRealizadas { get; set; }
    public int LimiteDiario { get; set; }
    public bool CircuitoAbierto { get; set; }
    public int LlamadasDisponibles => Math.Max(0, LimiteDiario - LlamadasRealizadas);
    public DateTime FechaActualizacion { get; set; }
}
