namespace Lugares.Application.DTOs;

public class FotoDto
{
    public Guid Id { get; set; }
    public string Url { get; set; } = string.Empty;
    public short Orden { get; set; }
    public bool EsPortada { get; set; }
}
